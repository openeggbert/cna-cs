using System.Collections.ObjectModel;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class SpriteFont
{
    private readonly CNA.Graphics.SpriteFont _font;
    private readonly ReadOnlyCollection<char> _characters;

    internal SpriteFont(
        Texture2D texture,
        IReadOnlyList<Rectangle> glyphBounds,
        IReadOnlyList<Rectangle> cropping,
        IReadOnlyList<char> characters,
        int lineSpacing,
        float spacing,
        IReadOnlyList<Vector3> kerning,
        char? defaultCharacter)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _font = new CNA.Graphics.SpriteFont(
            (CNA.Graphics.Texture2D)texture.FrameworkTexture,
            Convert(glyphBounds),
            Convert(cropping),
            characters,
            lineSpacing,
            spacing,
            Convert(kerning),
            defaultCharacter);
        _characters = new ReadOnlyCollection<char>([.. characters]);
    }

    internal CNA.Graphics.SpriteFont Framework => _font;

    public ReadOnlyCollection<char> Characters => _characters;

    public int LineSpacing
    {
        get => _font.LineSpacing;
        set => _font.LineSpacing = value;
    }

    public float Spacing
    {
        get => _font.Spacing;
        set => _font.Spacing = value;
    }

    public char? DefaultCharacter
    {
        get => _font.DefaultCharacter;
        set => _font.DefaultCharacter = value;
    }

    public Vector2 MeasureString(string text) => _font.MeasureString(text).ToCompat();

    public Vector2 MeasureString(StringBuilder text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _font.MeasureString(text.ToString()).ToCompat();
    }

    private static CNA.Rectangle[] Convert(IReadOnlyList<Rectangle> rectangles)
    {
        ArgumentNullException.ThrowIfNull(rectangles);
        var result = new CNA.Rectangle[rectangles.Count];
        for (int i = 0; i < rectangles.Count; i++)
        {
            result[i] = rectangles[i].ToFramework();
        }

        return result;
    }

    private static CNA.Vector3[] Convert(IReadOnlyList<Vector3> vectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        var result = new CNA.Vector3[vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            result[i] = vectors[i].ToFramework();
        }

        return result;
    }
}
