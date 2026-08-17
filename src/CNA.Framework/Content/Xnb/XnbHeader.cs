namespace CNA.Content.Xnb;

/// <summary>The compression scheme signaled by a real <c>.xnb</c> header's flags byte -- matches
/// the real openeggbert/cna C++ engine's own <c>XnbCompression</c> exactly (confirmed against FNA's
/// real <c>ContentManager.cs</c>: <c>0x80</c> = LZX, <c>0x40</c> = LZ4). This project reads only
/// <see cref="None"/>; <see cref="Lzx"/>/<see cref="Lz4"/> are real, valid, and detected, but
/// rejected with a clear <see cref="ContentLoadException"/> rather than decompressed -- a
/// deliberately deferred gap (LZX alone is a genuinely large, separable sub-feature), not an
/// oversight, matching this project's established "faithful subset, document the gap" style
/// (<c>SpriteFont</c>'s 256-glyph cap, <c>GamePad</c>'s partial <c>Buttons</c> flags).</summary>
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

        if (compression != XnbCompression.None)
        {
            throw new ContentLoadException(
                $"This .xnb file is {compression}-compressed -- only uncompressed .xnb files are supported so far.");
        }

        return new XnbHeader(platform, version, compression, totalLength);
    }
}
