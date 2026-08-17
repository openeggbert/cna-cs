namespace CNA.Content.Xnb;

/// <summary>
/// Decompresses a real <c>.xnb</c> file's LZX-compressed payload -- a direct port of the real
/// openeggbert/cna C++ engine's own <c>DecompressXnbPayload</c>
/// (<c>modules/content/src/Xnb/XnbDecompression.cpp</c>), matching FNA's own block-framing loop in
/// <c>ContentManager.GetContentReaderFromXnb</c> byte-for-byte.
///
/// The compressed payload is split into blocks, each normally decompressing to 32KB (<c>0x8000</c>)
/// unless a block explicitly overrides its own frame size (signaled by a leading <c>0xFF</c> byte,
/// followed by an explicit big-endian frame size and block size). Each block is fed to a single
/// <see cref="LzxDecoder"/> instance in turn -- the decoder's sliding window and repeated-offset
/// state persist across blocks within one file, matching LZX's own stateful, sequential-only
/// decompression model (there is no way to "jump to an offset" in an LZX-compressed stream).
/// </summary>
internal static class XnbLzxDecompression
{
    /// <summary>Matches the C++ port's own <c>XnbReadLimits.maxDecompressedSize</c> -- generous
    /// relative to any real <c>.xnb</c> file, not tuned per-fixture, the same "reject implausible
    /// counts before attempting the allocation" convention <see cref="XnbModelReader"/>'s own
    /// <c>MaxPlausibleCount</c> already established for this project's C# port.</summary>
    private const int MaxDecompressedSize = 256 * 1024 * 1024;

    /// <summary>Decompresses <paramref name="compressed"/> (the LZX-compressed payload immediately
    /// following a real <c>.xnb</c> file's 10-byte container header and 4-byte decompressed-size
    /// field) into exactly <paramref name="decompressedSize"/> bytes.</summary>
    /// <exception cref="ContentLoadException">Thrown if <paramref name="decompressedSize"/> is
    /// implausible, or if decompression fails or produces the wrong number of bytes.</exception>
    internal static byte[] Decompress(byte[] compressed, int decompressedSize, string assetName)
    {
        ArgumentNullException.ThrowIfNull(compressed);

        if (decompressedSize < 0 || decompressedSize > MaxDecompressedSize)
        {
            throw new ContentLoadException($"'{assetName}.xnb' has an invalid decompressed size ({decompressedSize}).");
        }

        using var compressedStream = new MemoryStream(compressed);
        using var decompressedStream = new MemoryStream();

        // Default window size for XNB-encoded files is 64KB (window exponent 16).
        var dec = new LzxDecoder(16);
        long pos = 0;

        while (pos < compressed.Length)
        {
            /* The compressed stream is separated into blocks that will decompress into 32KB or
             * some other size if specified. Normal, 32KB output blocks will have a short
             * indicating the size of the block before the block starts. Blocks that have a
             * defined output will be preceded by a byte of value 0xFF (255), then a short
             * indicating the output size and another for the block size. All shorts for these
             * cases are encoded in big-endian order. */
            int hi = compressedStream.ReadByte();
            int lo = compressedStream.ReadByte();
            int blockSize = (hi << 8) | lo;
            int frameSize = 0x8000; // Frame size is 32KB by default.
            if (hi == 0xFF)
            {
                hi = lo;
                lo = (byte)compressedStream.ReadByte();
                frameSize = (hi << 8) | lo;
                hi = (byte)compressedStream.ReadByte();
                lo = (byte)compressedStream.ReadByte();
                blockSize = (hi << 8) | lo;
                pos += 5;
            }
            else
            {
                pos += 2;
            }

            if (blockSize == 0 || frameSize == 0)
            {
                break;
            }

            if (dec.Decompress(compressedStream, blockSize, decompressedStream, frameSize) != 0)
            {
                throw new ContentLoadException($"Decompression of '{assetName}.xnb' failed.");
            }

            pos += blockSize;

            // Reset the position of the input just in case the bit buffer read in some unused bytes.
            compressedStream.Position = pos;
        }

        if (decompressedStream.Position != decompressedSize)
        {
            throw new ContentLoadException($"Decompression of '{assetName}.xnb' failed.");
        }

        return decompressedStream.ToArray();
    }
}
