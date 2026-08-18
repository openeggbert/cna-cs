using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// The parsed, native-free result of reading a real <c>.xnb</c> <c>Texture2DReader</c> object --
/// the same "return raw pieces, let the caller build the native-backed object" split
/// <see cref="XnbModelData"/> uses, and for the same reason: creating a
/// <see cref="Texture2D"/> needs a real <see cref="GraphicsDevice"/>, and keeping the parse free of
/// one is what makes it unit-testable without <c>cna-native</c>.
/// </summary>
internal sealed class XnbTextureData
{
    internal XnbTextureData(SurfaceFormat format, int width, int height, IReadOnlyList<byte[]> mipLevels)
    {
        Format = format;
        Width = width;
        Height = height;
        MipLevels = mipLevels;
    }

    internal SurfaceFormat Format { get; }

    internal int Width { get; }

    internal int Height { get; }

    /// <summary>Level 0 first. A <c>.xnb</c> texture always has at least one level.</summary>
    internal IReadOnlyList<byte[]> MipLevels { get; }
}
