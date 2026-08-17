namespace CNA.Content.Xnb;

/// <summary>The compression scheme signaled by a real <c>.xnb</c> header's flags byte -- matches
/// the real openeggbert/cna C++ engine's own <c>XnbCompression</c> exactly (confirmed against FNA's
/// real <c>ContentManager.cs</c>: <c>0x80</c> = LZX, <c>0x40</c> = LZ4). <see cref="None"/> and
/// <see cref="Lzx"/> are both real, supported values (see <see cref="XnbLzxDecompression"/> for the
/// latter, a direct port of the real C++ engine's own <c>LzxDecoder</c>). <see cref="Lz4"/> is real,
/// valid, and detected, but rejected with a clear <see cref="ContentLoadException"/> rather than
/// decompressed -- confirmed (against the real C++ engine's own source, which independently reached
/// the same conclusion) to be a MonoGame-only extension original XNA/FNA never produced or read, with
/// no byte-level framing details grounded anywhere reachable to implement it correctly, unlike LZX --
/// matching this project's established "faithful subset, document the gap" style (<c>SpriteFont</c>'s
/// 256-glyph cap, <c>GamePad</c>'s partial <c>Buttons</c> flags).</summary>
internal enum XnbCompression
{
    None,
    Lzx,
    Lz4,
}

/// <summary>Parsed fields of a real <c>.xnb</c> binary container header -- matches the real
/// openeggbert/cna C++ engine's own <c>XnbHeader</c>/<c>ParseXnbHeader</c> exactly, confirmed
/// byte-for-byte against a real, uncompressed, MonoGame-compiled <c>Model</c> asset.</summary>
internal readonly record struct XnbHeader(char Platform, int Version, XnbCompression Compression, int TotalLength)
{
    /// <summary>Byte offset, from the start of the file, where an LZX-compressed <c>.xnb</c> file's
    /// compressed payload begins: the 10-byte container header plus the 4-byte decompressed-size
    /// field that immediately follows it for <see cref="XnbCompression.Lzx"/> files only. A named
    /// constant (a code-review finding caught the raw literal <c>14</c> duplicated across
    /// <c>ContentManager.LoadXnbModelData</c> and its tests) rather than a magic number, so a future
    /// change to either field's size only needs updating here.</summary>
    internal const int LzxPayloadOffset = 14;

    /// <summary>Reads and validates the 10-byte header at the current stream position. Cross-checks
    /// <see cref="TotalLength"/> against <paramref name="streamLength"/> so a truncated/corrupt file
    /// fails clearly here rather than partway through object-graph parsing.</summary>
    internal static XnbHeader Read(BinaryReader reader, long streamLength)
    {
        ArgumentNullException.ThrowIfNull(reader);

        byte[] magic = reader.ReadBytes(3);
        if (magic.Length != 3 || magic[0] != (byte)'X' || magic[1] != (byte)'N' || magic[2] != (byte)'B')
        {
            throw new ContentLoadException("Not a valid .xnb file: missing 'XNB' magic bytes.");
        }

        char platform = (char)reader.ReadByte();
        int version = reader.ReadByte();
        if (version is not (4 or 5))
        {
            throw new ContentLoadException($"Unsupported .xnb version {version} (expected 4 or 5).");
        }

        byte flags = reader.ReadByte();
        XnbCompression compression = (flags & 0x80) != 0
            ? ((flags & 0x40) != 0 ? throw new ContentLoadException("Invalid .xnb flags byte: both compression bits set.") : XnbCompression.Lzx)
            : (flags & 0x40) != 0 ? XnbCompression.Lz4 : XnbCompression.None;

        int totalLength = reader.ReadInt32();
        if (totalLength != streamLength)
        {
            throw new ContentLoadException(
                $"Corrupt or truncated .xnb file: header declares {totalLength} bytes, but the file is {streamLength} bytes.");
        }

        if (compression == XnbCompression.Lz4)
        {
            throw new ContentLoadException(
                "This .xnb file uses MonoGame's Lz4 compression, which is not supported (a MonoGame-only " +
                "extension original XNA/FNA never produced or read, with no local format grounding to implement it correctly).");
        }

        return new XnbHeader(platform, version, compression, totalLength);
    }
}
