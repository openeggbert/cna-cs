using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// The parsed, native-free result of reading a real <c>.xnb</c> <c>SpriteFontReader</c> object.
/// Split from the built <see cref="SpriteFont"/> for the reason
/// <see cref="XnbModelData"/> records: the atlas has to become a real
/// <see cref="Texture2D"/>, which needs a <see cref="GraphicsDevice"/> this parse deliberately does
/// not take.
/// </summary>
internal sealed class XnbSpriteFontData
{
    internal XnbSpriteFontData(
        XnbTextureData texture,
        IReadOnlyList<Rectangle> glyphBounds,
        IReadOnlyList<Rectangle> cropping,
        IReadOnlyList<char> characters,
        int lineSpacing,
        float spacing,
        IReadOnlyList<Vector3> kerning,
        char? defaultCharacter)
    {
        Texture = texture;
        GlyphBounds = glyphBounds;
        Cropping = cropping;
        Characters = characters;
        LineSpacing = lineSpacing;
        Spacing = spacing;
        Kerning = kerning;
        DefaultCharacter = defaultCharacter;
    }

    internal XnbTextureData Texture { get; }

    internal IReadOnlyList<Rectangle> GlyphBounds { get; }

    internal IReadOnlyList<Rectangle> Cropping { get; }

    internal IReadOnlyList<char> Characters { get; }

    internal int LineSpacing { get; }

    internal float Spacing { get; }

    internal IReadOnlyList<Vector3> Kerning { get; }

    internal char? DefaultCharacter { get; }
}
