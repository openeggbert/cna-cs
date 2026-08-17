namespace CNA.Content.Xnb;

/// <summary>
/// LZX decompressor for real <c>.xnb</c> container payloads -- a direct, line-by-line C# port of
/// the real openeggbert/cna C++ engine's own <c>LzxDecoder</c> (<c>modules/content/src/Xnb/LzxDecoder.cpp</c>),
/// which is itself a from-scratch C++ port of FNA's <c>LzxDecoder.cs</c> (itself a C# port of
/// libmspack's <c>lzxd.c</c>, Copyright 2003-2004 Stuart Caie / 2011 Ali Scissons, dual MSPL/LGPL --
/// used here under CNA's own MS-PL license, matching FNA's and the C++ port's own use). Field names
/// map 1:1 onto the C++ port's own <c>LzxState</c> members (<c>R0</c>/<c>R1</c>/<c>R2</c> etc.,
/// renamed to this project's own <c>_camelCase</c> private-field convention) and every method below
/// preserves the C++ port's exact control flow -- this is a faithful port, not a reimplementation
/// from the algorithm's description, specifically so it stays verifiable against the same reference
/// (and the same real, independently-produced, byte-exact decompressed fixtures the C++ port itself
/// is cross-checked against -- see <c>XnbLzxDecompressionTests.cs</c>).
///
/// One instance holds decoder <em>state</em> across multiple <see cref="Decompress"/> calls within
/// the same <c>.xnb</c> file's block stream (the sliding window, repeated-offset LRU queue, and
/// Huffman tables all persist between blocks) -- construct a fresh instance per file, never share
/// one across files, matching the C++ port's own documented requirement.
///
/// "Intel E8" call-address translation (a general LZX/CAB feature for compressing x86 executable
/// code, irrelevant to game asset payloads) is <b>not</b> completed here, matching the C++ port's own
/// verbatim reproduction of FNA's own unfinished original: real <c>.xnb</c> content encoded with
/// this option already fails identically against real FNA, so "finishing" it here would diverge
/// from, not match, the reference this port is grounded against. In practice this option is
/// essentially never set for ordinary game-asset <c>.xnb</c> files (it exists for compressing
/// executable code inside CAB archives).
/// </summary>
internal sealed class LzxDecoder
{
    private const int MinMatch = 2;
    private const int NumChars = 256;
    private const int PretreeNumElements = 20;
    private const int AlignedNumElements = 8;
    private const int NumPrimaryLengths = 7;
    private const int NumSecondaryLengths = 249;

    private const int PretreeMaxSymbols = PretreeNumElements;
    private const int PretreeTableBits = 6;
    private const int MaintreeMaxSymbols = NumChars + 50 * 8;
    private const int MaintreeTableBits = 12;
    private const int LengthMaxSymbols = NumSecondaryLengths + 1;
    private const int LengthTableBits = 12;
    private const int AlignedMaxSymbols = AlignedNumElements;
    private const int AlignedTableBits = 7;
    private const int LentableSafety = 64;

    // FNA's own static (lazily-initialized-once, shared across every LzxDecoder instance) lookup
    // tables -- C#'s own static field initializers give the same "compute once, thread-safely"
    // behavior the C++ port's "magic statics" do.
    private static readonly byte[] ExtraBits = BuildExtraBits();
    private static readonly uint[] PositionBase = BuildPositionBase();

    private enum BlockType : byte
    {
        Invalid = 0,
        Verbatim = 1,
        Aligned = 2,
        Uncompressed = 3,
    }

    private uint _r0;
    private uint _r1;
    private uint _r2;
    private readonly ushort _mainElements;
    private bool _headerRead;
    private BlockType _blockType = BlockType.Invalid;
    private uint _blockLength;
    private uint _blockRemaining;
    private uint _framesRead;
    private int _intelFilesize;
    private int _intelCurpos;
    private bool _intelStarted;

    private readonly ushort[] _pretreeTable;
    private readonly byte[] _pretreeLen;
    private readonly ushort[] _maintreeTable;
    private readonly byte[] _maintreeLen;
    private readonly ushort[] _lengthTable;
    private readonly byte[] _lengthLen;
    private readonly ushort[] _alignedTable;
    private readonly byte[] _alignedLen;

    private readonly byte[] _window;
    private readonly uint _windowSize;
    private uint _windowPosn;

    /// <summary>Constructs a decoder with a 2^<paramref name="window"/>-byte sliding window.</summary>
    /// <param name="window">Window size exponent, 15-21 (<c>.xnb</c> always uses 16, i.e. 64KB).</param>
    /// <exception cref="ContentLoadException">Thrown if <paramref name="window"/> is outside [15, 21]
    /// (matching FNA's own <c>UnsupportedLzxWindowSizeRange</c>, reported through this project's own
    /// single-exception-type-for-content-errors convention instead).</exception>
    internal LzxDecoder(int window)
    {
        if (window < 15 || window > 21)
        {
            throw new ContentLoadException("Unsupported LZX window size (must be 15-21).");
        }

        uint wndsize = 1u << window;
        _window = new byte[wndsize];
        Array.Fill(_window, (byte)0xDC);
        _windowSize = wndsize;
        _windowPosn = 0;

        int posnSlots;
        if (window == 20)
        {
            posnSlots = 42;
        }
        else if (window == 21)
        {
            posnSlots = 50;
        }
        else
        {
            posnSlots = window << 1;
        }

        _r0 = _r1 = _r2 = 1;
        _mainElements = (ushort)(NumChars + (posnSlots << 3));
        _headerRead = false;
        _framesRead = 0;
        _blockRemaining = 0;
        _blockType = BlockType.Invalid;
        _intelCurpos = 0;
        _intelStarted = false;

        _pretreeTable = new ushort[(1u << PretreeTableBits) + ((uint)PretreeMaxSymbols << 1)];
        _pretreeLen = new byte[PretreeMaxSymbols + LentableSafety];
        _maintreeTable = new ushort[(1u << MaintreeTableBits) + ((uint)MaintreeMaxSymbols << 1)];
        _maintreeLen = new byte[MaintreeMaxSymbols + LentableSafety];
        _lengthTable = new ushort[(1u << LengthTableBits) + ((uint)LengthMaxSymbols << 1)];
        _lengthLen = new byte[LengthMaxSymbols + LentableSafety];
        _alignedTable = new ushort[(1u << AlignedTableBits) + ((uint)AlignedMaxSymbols << 1)];
        _alignedLen = new byte[AlignedMaxSymbols + LentableSafety];
    }

    private static byte[] BuildExtraBits()
    {
        var bits = new byte[52];
        for (int i = 0, j = 0; i <= 50; i += 2)
        {
            bits[i] = bits[i + 1] = (byte)j;
            if (i != 0 && j < 17)
            {
                j++;
            }
        }

        return bits;
    }

    private static uint[] BuildPositionBase()
    {
        var positionBase = new uint[51];
        for (int i = 0, j = 0; i <= 50; i++)
        {
            positionBase[i] = (uint)j;
            j += 1 << ExtraBits[i];
        }

        return positionBase;
    }

    private static int MakeDecodeTable(int nsyms, int nbits, byte[] length, ushort[] table)
    {
        ushort sym;
        uint leaf;
        byte bitNum = 1;
        uint fill;
        uint pos = 0;
        uint tableMask = 1u << nbits;
        uint bitMask = tableMask >> 1;
        uint nextSymbol = bitMask;

        while (bitNum <= nbits)
        {
            for (sym = 0; sym < nsyms; sym++)
            {
                if (length[sym] == bitNum)
                {
                    leaf = pos;
                    if ((pos += bitMask) > tableMask)
                    {
                        return 1;
                    }

                    fill = bitMask;
                    while (fill-- > 0)
                    {
                        table[leaf++] = sym;
                    }
                }
            }

            bitMask >>= 1;
            bitNum++;
        }

        if (pos != tableMask)
        {
            for (sym = (ushort)pos; sym < tableMask; sym++)
            {
                table[sym] = 0;
            }

            pos <<= 16;
            tableMask <<= 16;
            bitMask = 1u << 15;

            while (bitNum <= 16)
            {
                for (sym = 0; sym < nsyms; sym++)
                {
                    if (length[sym] == bitNum)
                    {
                        leaf = pos >> 16;
                        for (fill = 0; fill < (uint)bitNum - nbits; fill++)
                        {
                            // A code-review-shaped hardening the C++ port itself added (a real
                            // heap-buffer-overflow was found and fixed here via fuzzing, per that
                            // port's own history) -- a corrupt/adversarial Huffman code-length
                            // table can grow next_symbol/leaf past this table's allocated room.
                            // C#'s own array indexing would already throw IndexOutOfRangeException
                            // here regardless, but this explicit check keeps the same
                            // "reject cleanly with 1, never partially write" contract the rest of
                            // this method's callers rely on.
                            if (leaf >= (uint)table.Length || ((ulong)nextSymbol << 1) + 1 >= (uint)table.Length)
                            {
                                return 1;
                            }

                            if (table[leaf] == 0)
                            {
                                table[nextSymbol << 1] = 0;
                                table[(nextSymbol << 1) + 1] = 0;
                                table[leaf] = (ushort)nextSymbol++;
                            }

                            leaf = (uint)table[leaf] << 1;
                            if (((pos >> (15 - (int)fill)) & 1) == 1)
                            {
                                leaf++;
                            }
                        }

                        if (leaf >= (uint)table.Length)
                        {
                            return 1;
                        }

                        table[leaf] = sym;

                        if ((pos += bitMask) > tableMask)
                        {
                            return 1;
                        }
                    }
                }

                bitMask >>= 1;
                bitNum++;
            }
        }

        if (pos == tableMask)
        {
            return 0;
        }

        for (sym = 0; sym < nsyms; sym++)
        {
            if (length[sym] != 0)
            {
                return 1;
            }
        }

        return 0;
    }

    /// <summary>Returns 0 on success, 1 if the pretree's own <see cref="MakeDecodeTable"/> call
    /// failed (a corrupt/adversarial code-length table that doesn't form a complete canonical
    /// Huffman code) -- a code-review finding caught this failure previously being silently
    /// discarded at every one of this method's own callers (matching this method's own, and the
    /// reference C++/C# implementations', original control flow -- <see cref="MakeDecodeTable"/>'s
    /// failure return was never checked anywhere it's called), which risked <see cref="ReadHuffSym"/>
    /// silently decoding against a partially-built table (its own out-of-range fallback returns
    /// symbol 0 rather than throwing) instead of failing cleanly. <see cref="Decompress"/>'s own
    /// three call sites now check this return value the same way it already checks every other
    /// error condition in that method (0 = success, negative = error).</summary>
    private int ReadLengths(byte[] lens, uint first, uint last, BitBuffer bitbuf)
    {
        for (uint x = 0; x < 20; x++)
        {
            uint y = bitbuf.ReadBits(4);
            _pretreeLen[x] = (byte)y;
        }

        if (MakeDecodeTable(PretreeMaxSymbols, PretreeTableBits, _pretreeLen, _pretreeTable) != 0)
        {
            return 1;
        }

        for (uint x = first; x < last;)
        {
            int z = (int)ReadHuffSym(_pretreeTable, _pretreeLen, PretreeMaxSymbols, PretreeTableBits, bitbuf);
            if (z == 17)
            {
                uint y = bitbuf.ReadBits(4);
                y += 4;
                while (y-- != 0)
                {
                    lens[x++] = 0;
                }
            }
            else if (z == 18)
            {
                uint y = bitbuf.ReadBits(5);
                y += 20;
                while (y-- != 0)
                {
                    lens[x++] = 0;
                }
            }
            else if (z == 19)
            {
                uint y = bitbuf.ReadBits(1);
                y += 4;
                z = (int)ReadHuffSym(_pretreeTable, _pretreeLen, PretreeMaxSymbols, PretreeTableBits, bitbuf);
                z = lens[x] - z;
                if (z < 0)
                {
                    z += 17;
                }

                while (y-- != 0)
                {
                    lens[x++] = (byte)z;
                }
            }
            else
            {
                z = lens[x] - z;
                if (z < 0)
                {
                    z += 17;
                }

                lens[x++] = (byte)z;
            }
        }

        return 0;
    }

    private static uint ReadHuffSym(ushort[] table, byte[] lengths, int nsyms, int nbits, BitBuffer bitbuf)
    {
        bitbuf.EnsureBits(16);
        uint i;
        if ((i = table[bitbuf.PeekBits((byte)nbits)]) >= nsyms)
        {
            uint j = 1u << (32 - nbits);
            do
            {
                j >>= 1;
                i <<= 1;
                i |= (bitbuf.GetBuffer() & j) != 0 ? 1u : 0u;
                if (j == 0)
                {
                    return 0; // matches FNA's own "TODO: throw proper exception"
                }
            }
            while ((i = table[i]) >= nsyms);
        }

        uint lengthBits = lengths[i];
        bitbuf.RemoveBits((byte)lengthBits);

        return i;
    }

    /// <summary>Decompresses one block from <paramref name="inData"/> into <paramref name="outData"/>.</summary>
    /// <param name="inData">Compressed input stream, positioned at the start of this block.</param>
    /// <param name="inLen">Number of compressed bytes this block occupies in <paramref name="inData"/>.</param>
    /// <param name="outData">Output stream to append <paramref name="outLen"/> decompressed bytes to.</param>
    /// <param name="outLen">Number of decompressed bytes this block should produce.</param>
    /// <returns>0 on success, negative on any error (matches FNA's/the C++ port's own error-as-return-code style).</returns>
    internal int Decompress(Stream inData, int inLen, Stream outData, int outLen)
    {
        var bitbuf = new BitBuffer(inData);
        long startpos = inData.Position;

        byte[] window = _window;

        uint windowPosn = _windowPosn;
        uint windowSize = _windowSize;
        uint r0 = _r0;
        uint r1 = _r1;
        uint r2 = _r2;
        uint i, j;

        int togo = outLen;
        int thisRun, mainElement, matchLength, matchOffset, lengthFooter, extra, verbatimBits;
        int rundest, runsrc, copyLength, alignedBits;

        bitbuf.InitBitStream();

        // Read header if necessary.
        if (!_headerRead)
        {
            uint intel = bitbuf.ReadBits(1);
            if (intel != 0)
            {
                i = bitbuf.ReadBits(16);
                j = bitbuf.ReadBits(16);
                _intelFilesize = (int)((i << 16) | j);
            }

            _headerRead = true;
        }

        // Main decoding loop.
        while (togo > 0)
        {
            // Last block finished, new block expected.
            if (_blockRemaining == 0)
            {
                if (_blockType == BlockType.Uncompressed)
                {
                    if ((_blockLength & 1) == 1)
                    {
                        inData.ReadByte(); // realign bitstream to word
                    }

                    bitbuf.InitBitStream();
                }

                _blockType = (BlockType)bitbuf.ReadBits(3);
                i = bitbuf.ReadBits(16);
                j = bitbuf.ReadBits(8);
                _blockRemaining = _blockLength = (i << 8) | j;

                switch (_blockType)
                {
                    case BlockType.Aligned:
                        for (i = 0, j = 0; i < 8; i++)
                        {
                            j = bitbuf.ReadBits(3);
                            _alignedLen[i] = (byte)j;
                        }

                        // A code-review finding caught MakeDecodeTable's failure return value being
                        // silently discarded at every call site in this switch (matching the
                        // reference C++/C# implementations' own control flow, but inconsistent with
                        // every other error condition in this method, which all propagate cleanly)
                        // -- checked here and below now, the same "0 = success, negative = error"
                        // convention already used throughout Decompress.
                        if (MakeDecodeTable(AlignedMaxSymbols, AlignedTableBits, _alignedLen, _alignedTable) != 0)
                        {
                            return -1;
                        }

                        // Rest of aligned header is same as verbatim.
                        goto case BlockType.Verbatim;

                    case BlockType.Verbatim:
                        if (ReadLengths(_maintreeLen, 0, 256, bitbuf) != 0 ||
                            ReadLengths(_maintreeLen, 256, _mainElements, bitbuf) != 0)
                        {
                            return -1;
                        }

                        if (MakeDecodeTable(MaintreeMaxSymbols, MaintreeTableBits, _maintreeLen, _maintreeTable) != 0)
                        {
                            return -1;
                        }

                        if (_maintreeLen[0xE8] != 0)
                        {
                            _intelStarted = true;
                        }

                        if (ReadLengths(_lengthLen, 0, NumSecondaryLengths, bitbuf) != 0)
                        {
                            return -1;
                        }

                        if (MakeDecodeTable(LengthMaxSymbols, LengthTableBits, _lengthLen, _lengthTable) != 0)
                        {
                            return -1;
                        }

                        break;

                    case BlockType.Uncompressed:
                    {
                        _intelStarted = true; // Because we can't assume otherwise.
                        bitbuf.EnsureBits(16); // Get up to 16 pad bits into the buffer.
                        if (bitbuf.GetBitsLeft() > 16)
                        {
                            inData.Seek(-2, SeekOrigin.Current);
                        }

                        byte hi, mh, ml, lo;
                        lo = (byte)inData.ReadByte();
                        ml = (byte)inData.ReadByte();
                        mh = (byte)inData.ReadByte();
                        hi = (byte)inData.ReadByte();
                        r0 = (uint)(lo | (ml << 8) | (mh << 16) | (hi << 24));
                        lo = (byte)inData.ReadByte();
                        ml = (byte)inData.ReadByte();
                        mh = (byte)inData.ReadByte();
                        hi = (byte)inData.ReadByte();
                        r1 = (uint)(lo | (ml << 8) | (mh << 16) | (hi << 24));
                        lo = (byte)inData.ReadByte();
                        ml = (byte)inData.ReadByte();
                        mh = (byte)inData.ReadByte();
                        hi = (byte)inData.ReadByte();
                        r2 = (uint)(lo | (ml << 8) | (mh << 16) | (hi << 24));
                        break;
                    }

                    default:
                        return -1;
                }
            }

            // Buffer exhaustion check.
            if (inData.Position > startpos + inLen)
            {
                /* It's possible to have a file where the next run is less than
                 * 16 bits in size. In this case, the READ_HUFFSYM() macro used
                 * in building the tables will exhaust the buffer, so we should
                 * allow for this, but not allow those accidentally read bits to
                 * be used (so we check that there are at least 16 bits
                 * remaining - in this boundary case they aren't really part of
                 * the compressed data).
                 */
                if (inData.Position > startpos + inLen + 2 || bitbuf.GetBitsLeft() < 16)
                {
                    return -1;
                }
            }

            while ((thisRun = (int)_blockRemaining) > 0 && togo > 0)
            {
                if (thisRun > togo)
                {
                    thisRun = togo;
                }

                togo -= thisRun;
                _blockRemaining -= (uint)thisRun;

                // Apply 2^x-1 mask.
                windowPosn &= windowSize - 1;
                // Runs can't straddle the window wraparound.
                if (windowPosn + (uint)thisRun > windowSize)
                {
                    return -1;
                }

                switch (_blockType)
                {
                    case BlockType.Verbatim:
                        while (thisRun > 0)
                        {
                            mainElement = (int)ReadHuffSym(_maintreeTable, _maintreeLen, MaintreeMaxSymbols, MaintreeTableBits, bitbuf);
                            if (mainElement < NumChars)
                            {
                                // Literal: 0 to NUM_CHARS-1.
                                window[windowPosn++] = (byte)mainElement;
                                thisRun--;
                            }
                            else
                            {
                                // Match: NUM_CHARS + ((slot<<3) | length_header (3 bits)).
                                mainElement -= NumChars;

                                matchLength = mainElement & NumPrimaryLengths;
                                if (matchLength == NumPrimaryLengths)
                                {
                                    lengthFooter = (int)ReadHuffSym(_lengthTable, _lengthLen, LengthMaxSymbols, LengthTableBits, bitbuf);
                                    matchLength += lengthFooter;
                                }

                                matchLength += MinMatch;

                                matchOffset = mainElement >> 3;

                                if (matchOffset > 2)
                                {
                                    // Not repeated offset.
                                    if (matchOffset != 3)
                                    {
                                        extra = ExtraBits[matchOffset];
                                        verbatimBits = (int)bitbuf.ReadBits((byte)extra);
                                        matchOffset = (int)PositionBase[matchOffset] - 2 + verbatimBits;
                                    }
                                    else
                                    {
                                        matchOffset = 1;
                                    }

                                    // Update repeated offset LRU queue.
                                    r2 = r1;
                                    r1 = r0;
                                    r0 = (uint)matchOffset;
                                }
                                else if (matchOffset == 0)
                                {
                                    matchOffset = (int)r0;
                                }
                                else if (matchOffset == 1)
                                {
                                    matchOffset = (int)r1;
                                    r1 = r0;
                                    r0 = (uint)matchOffset;
                                }
                                else // matchOffset == 2
                                {
                                    matchOffset = (int)r2;
                                    r2 = r0;
                                    r0 = (uint)matchOffset;
                                }

                                // Same hardening as MakeDecodeTable's own -- reject a corrupt
                                // match_offset explicitly instead of an out-of-bounds window read.
                                if (matchOffset <= 0 || matchOffset > windowSize)
                                {
                                    return -1;
                                }

                                rundest = (int)windowPosn;
                                thisRun -= matchLength;

                                // Copy any wrapped around source data.
                                if (windowPosn >= matchOffset)
                                {
                                    // No wrap.
                                    runsrc = rundest - matchOffset;
                                }
                                else
                                {
                                    runsrc = rundest + ((int)windowSize - matchOffset);
                                    copyLength = matchOffset - (int)windowPosn;
                                    if (copyLength < matchLength)
                                    {
                                        matchLength -= copyLength;
                                        windowPosn += (uint)copyLength;
                                        while (copyLength-- > 0)
                                        {
                                            window[rundest++] = window[runsrc++];
                                        }

                                        runsrc = 0;
                                    }
                                }

                                windowPosn += (uint)matchLength;

                                // Copy match data - no worries about destination wraps.
                                while (matchLength-- > 0)
                                {
                                    window[rundest++] = window[runsrc++];
                                }
                            }
                        }

                        break;

                    case BlockType.Aligned:
                        while (thisRun > 0)
                        {
                            mainElement = (int)ReadHuffSym(_maintreeTable, _maintreeLen, MaintreeMaxSymbols, MaintreeTableBits, bitbuf);

                            if (mainElement < NumChars)
                            {
                                // Literal 0 to NUM_CHARS-1.
                                window[windowPosn++] = (byte)mainElement;
                                thisRun -= 1;
                            }
                            else
                            {
                                // Match: NUM_CHARS + ((slot<<3) | length_header (3 bits)).
                                mainElement -= NumChars;

                                matchLength = mainElement & NumPrimaryLengths;
                                if (matchLength == NumPrimaryLengths)
                                {
                                    lengthFooter = (int)ReadHuffSym(_lengthTable, _lengthLen, LengthMaxSymbols, LengthTableBits, bitbuf);
                                    matchLength += lengthFooter;
                                }

                                matchLength += MinMatch;

                                matchOffset = mainElement >> 3;

                                if (matchOffset > 2)
                                {
                                    // Not repeated offset.
                                    extra = ExtraBits[matchOffset];
                                    matchOffset = (int)PositionBase[matchOffset] - 2;
                                    if (extra > 3)
                                    {
                                        // Verbatim and aligned bits.
                                        extra -= 3;
                                        verbatimBits = (int)bitbuf.ReadBits((byte)extra);
                                        matchOffset += verbatimBits << 3;
                                        alignedBits = (int)ReadHuffSym(_alignedTable, _alignedLen, AlignedMaxSymbols, AlignedTableBits, bitbuf);
                                        matchOffset += alignedBits;
                                    }
                                    else if (extra == 3)
                                    {
                                        // Aligned bits only.
                                        alignedBits = (int)ReadHuffSym(_alignedTable, _alignedLen, AlignedMaxSymbols, AlignedTableBits, bitbuf);
                                        matchOffset += alignedBits;
                                    }
                                    else if (extra > 0) // extra==1, extra==2
                                    {
                                        // Verbatim bits only.
                                        verbatimBits = (int)bitbuf.ReadBits((byte)extra);
                                        matchOffset += verbatimBits;
                                    }
                                    else // extra == 0
                                    {
                                        matchOffset = 1;
                                    }

                                    // Update repeated offset LRU queue.
                                    r2 = r1;
                                    r1 = r0;
                                    r0 = (uint)matchOffset;
                                }
                                else if (matchOffset == 0)
                                {
                                    matchOffset = (int)r0;
                                }
                                else if (matchOffset == 1)
                                {
                                    matchOffset = (int)r1;
                                    r1 = r0;
                                    r0 = (uint)matchOffset;
                                }
                                else // matchOffset == 2
                                {
                                    matchOffset = (int)r2;
                                    r2 = r0;
                                    r0 = (uint)matchOffset;
                                }

                                if (matchOffset <= 0 || matchOffset > windowSize)
                                {
                                    return -1;
                                }

                                rundest = (int)windowPosn;
                                thisRun -= matchLength;

                                // Copy any wrapped around source data.
                                if (windowPosn >= matchOffset)
                                {
                                    // No wrap.
                                    runsrc = rundest - matchOffset;
                                }
                                else
                                {
                                    runsrc = rundest + ((int)windowSize - matchOffset);
                                    copyLength = matchOffset - (int)windowPosn;
                                    if (copyLength < matchLength)
                                    {
                                        matchLength -= copyLength;
                                        windowPosn += (uint)copyLength;
                                        while (copyLength-- > 0)
                                        {
                                            window[rundest++] = window[runsrc++];
                                        }

                                        runsrc = 0;
                                    }
                                }

                                windowPosn += (uint)matchLength;

                                // Copy match data - no worries about destination wraps.
                                while (matchLength-- > 0)
                                {
                                    window[rundest++] = window[runsrc++];
                                }
                            }
                        }

                        break;

                    case BlockType.Uncompressed:
                    {
                        if (inData.Position + thisRun > startpos + inLen)
                        {
                            return -1;
                        }

                        byte[] tempBuffer = new byte[thisRun];
                        inData.ReadExactly(tempBuffer, 0, thisRun);
                        Array.Copy(tempBuffer, 0, window, windowPosn, thisRun);
                        windowPosn += (uint)thisRun;
                        break;
                    }

                    default:
                        return -1;
                }
            }
        }

        if (togo != 0)
        {
            return -1;
        }

        int startWindowPos = (int)windowPosn;
        if (startWindowPos == 0)
        {
            startWindowPos = (int)windowSize;
        }

        startWindowPos -= outLen;
        outData.Write(window, startWindowPos, outLen);

        _windowPosn = windowPosn;
        _r0 = r0;
        _r1 = r1;
        _r2 = r2;

        // Intel E8 decoding: FNA's own port never actually finished this (its loop condition never
        // advances outData's position, and it always returns -1 immediately below, matching its own
        // "TODO: Finish intel E8 decoding" comment) -- reproduced verbatim, not silently "fixed",
        // since real .xnb content encoded with this option would already fail identically against
        // real FNA and the C++ port this was itself ported from. This path is essentially
        // unreachable for ordinary game-asset .xnb files, since _intelFilesize only becomes nonzero
        // when the header's own "intel" bit is set, which no ordinary content-pipeline output does.
        if (_framesRead++ < 32768 && _intelFilesize != 0)
        {
            if (outLen <= 6 || !_intelStarted)
            {
                _intelCurpos += outLen;
            }
            else
            {
                int dataend = outLen - 10;
                uint curpos = (uint)_intelCurpos;
                _intelCurpos = (int)curpos + outLen;

                while (outData.Position < dataend)
                {
                    if (outData.ReadByte() != 0xE8)
                    {
                        curpos++;
                    }
                }
            }

            return -1;
        }

        return 0;
    }

    /// <summary>FNA's/the C++ port's own nested <c>BitBuffer</c> type -- reads the compressed stream
    /// 16 bits at a time into a 32-bit MSB-aligned buffer.</summary>
    private sealed class BitBuffer
    {
        private uint _buffer;
        private byte _bitsLeft;
        private readonly Stream _stream;

        internal BitBuffer(Stream stream)
        {
            _stream = stream;
            InitBitStream();
        }

        internal void InitBitStream()
        {
            _buffer = 0;
            _bitsLeft = 0;
        }

        internal void EnsureBits(byte bits)
        {
            while (_bitsLeft < bits)
            {
                // Matches FNA's/the C++ port's own (byte)stream.ReadByte(): at end-of-stream,
                // ReadByte() returns -1, which the (byte) cast wraps to 0xFF -- replicated verbatim
                // here, not treated as a special EOF case.
                byte lo = (byte)_stream.ReadByte();
                byte hi = (byte)_stream.ReadByte();
                _buffer |= (uint)((hi << 8) | lo) << (32 - 16 - _bitsLeft);
                _bitsLeft = (byte)(_bitsLeft + 16);
            }
        }

        internal uint PeekBits(byte bits) => _buffer >> (32 - bits);

        internal void RemoveBits(byte bits)
        {
            _buffer <<= bits;
            _bitsLeft = (byte)(_bitsLeft - bits);
        }

        internal uint ReadBits(byte bits)
        {
            uint ret = 0;
            if (bits > 0)
            {
                EnsureBits(bits);
                ret = PeekBits(bits);
                RemoveBits(bits);
            }

            return ret;
        }

        internal uint GetBuffer() => _buffer;

        internal byte GetBitsLeft() => _bitsLeft;
    }
}
