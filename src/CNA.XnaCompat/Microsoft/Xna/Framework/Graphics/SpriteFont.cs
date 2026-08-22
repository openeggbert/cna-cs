namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>SpriteFont</c>. <c>Characters</c>/<c>LineSpacing</c>/<c>Spacing</c>/
/// <c>DefaultCharacter</c>/<c>MeasureString</c> are inherited unchanged from
/// <see cref="CNA.Graphics.SpriteFont"/> -- <c>MeasureString</c>'s <c>CNA.Vector2</c> return value
/// converts through <c>Vector2</c>'s implicit operator at the call site, same as every other
/// CNA.Framework method returning a math value type. <c>Texture</c> needs a `new` override
/// (see below) because it is the one property whose *declared* type actually differs here.
/// </summary>
public class SpriteFont : CNA.Graphics.SpriteFont
{
    public SpriteFont(
        Texture2D texture,
        IReadOnlyList<Rectangle> glyphBounds,
        IReadOnlyList<Rectangle> cropping,
        IReadOnlyList<char> characters,
        int lineSpacing,
        float spacing,
        IReadOnlyList<Vector3> kerning,
        char? defaultCharacter)
        : base(
            (CNA.Graphics.Texture2D)texture.FrameworkTexture,
            Convert(glyphBounds),
            Convert(cropping),
            characters,
            lineSpacing,
            spacing,
            Convert(kerning),
            defaultCharacter)
    {
        Texture = texture;
    }

    /// <summary>
    /// Hides the base <c>Texture</c> property (declared <c>CNA.Graphics.Texture2D</c>) with this
    /// namespace's own <c>Texture2D</c>. The base property's *value* is always actually a
    /// compat-layer <c>Texture2D</c> instance too (the constructor only ever passes one in via
    /// upcast) -- this override just gives callers a variable they can assign to a
    /// compat-typed <c>Texture2D</c> field without an explicit cast, the same convenience real
    /// XNA's own <c>Texture</c> property provides.
    /// </summary>
    public new Texture2D Texture { get; }

    /// <summary>List&lt;T&gt;/array element types (<c>Rectangle</c>, <c>Vector3</c>) do not
    /// implicitly convert as a collection even though the elements do -- C# generics are
    /// invariant and enums/structs get no collection-level conversion operator, so each glyph
    /// array needs an explicit element-wise conversion before it can reach the base
    /// constructor.</summary>
    private static CNA.Rectangle[] Convert(IReadOnlyList<Rectangle> rectangles)
    {
        var result = new CNA.Rectangle[rectangles.Count];
        for (int i = 0; i < rectangles.Count; i++)
        {
            result[i] = rectangles[i];
        }

        return result;
    }

    private static CNA.Vector3[] Convert(IReadOnlyList<Vector3> vectors)
    {
        var result = new CNA.Vector3[vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            result[i] = vectors[i];
        }

        return result;
    }
}
