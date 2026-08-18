using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>Texture2DReader</c> record: surface format, dimensions, mip count,
/// then one length-prefixed byte block per level.
///
/// The layout is XNA's, read out of the format rather than guessed -- a <see cref="SurfaceFormat"/>
/// as <see cref="int"/>, then three <see cref="uint"/>s (width, height, level count), then for each
/// level a <see cref="uint"/> byte count followed by exactly that many bytes.
/// </summary>
internal static class XnbTexture2DReader
{
    /// <summary>A texture larger than this is rejected rather than trusted. Not a format limit --
    /// a corrupt-file guard, matching the "implausible count" checks the rest of this reader family
    /// already makes: the counts below are attacker-controlled byte counts that would otherwise
    /// become allocation sizes.</summary>
    private const uint MaxDimension = 65536;

    private const uint MaxMipLevels = 32;

    private const uint MaxLevelBytes = 256 * 1024 * 1024;

    internal static object Read(XnbContentReader reader)
    {
        int formatValue = reader.ReadInt32();
        if (!Enum.IsDefined(typeof(SurfaceFormat), formatValue))
        {
            throw new ContentLoadException($"Corrupt .xnb texture: unknown surface format {formatValue}.");
        }

        uint width = reader.ReadUInt32();
        uint height = reader.ReadUInt32();
        uint levelCount = reader.ReadUInt32();

        if (width == 0 || height == 0 || width > MaxDimension || height > MaxDimension)
        {
            throw new ContentLoadException($"Corrupt .xnb texture: implausible dimensions {width}x{height}.");
        }

        if (levelCount == 0 || levelCount > MaxMipLevels)
        {
            throw new ContentLoadException($"Corrupt .xnb texture: implausible mip level count {levelCount}.");
        }

        var levels = new byte[levelCount][];
        for (uint i = 0; i < levelCount; i++)
        {
            uint byteCount = reader.ReadUInt32();
            if (byteCount > MaxLevelBytes)
            {
                throw new ContentLoadException($"Corrupt .xnb texture: implausible mip level size {byteCount} bytes.");
            }

            levels[i] = reader.ReadExactBytes((int)byteCount);
        }

        return new XnbTextureData((SurfaceFormat)formatValue, (int)width, (int)height, levels);
    }
}
