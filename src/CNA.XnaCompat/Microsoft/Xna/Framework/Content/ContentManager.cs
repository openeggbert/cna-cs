namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// XNA 4.0-compatible <c>ContentManager</c>. Overrides <c>Load&lt;T&gt;</c> to recognize compat
/// content types (currently <see cref="Graphics.Texture2D"/>), reusing the base class's protected
/// native-load helper so this project never references CNA.Interop directly. See
/// ../../../../../docs/architecture.md and ../../../../../docs/xna-compatibility.md.
/// </summary>
public class ContentManager : CNA.Content.ContentManager
{
    protected internal ContentManager(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    public override T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Graphics.Texture2D))
        {
            return (T)(object)new Graphics.Texture2D(LoadNativeTexture2DHandle(assetName));
        }

        if (typeof(T) == typeof(Graphics.SpriteFont))
        {
            SpriteFontData data = LoadSpriteFontData(assetName);
            return (T)(object)new Graphics.SpriteFont(
                new Graphics.Texture2D(data.TextureHandle),
                Convert(data.GlyphBounds),
                Convert(data.Cropping),
                data.Characters,
                data.LineSpacing,
                data.Spacing,
                Convert(data.Kerning),
                data.DefaultCharacter);
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }

    /// <summary>Element-wise conversion, not a collection-level one -- see the identical pattern
    /// (in the opposite direction) in <c>Microsoft.Xna.Framework.Graphics.SpriteFont</c>'s own
    /// constructor for why C# generics can't do this automatically even though the elements
    /// convert.</summary>
    private static Rectangle[] Convert(IReadOnlyList<CNA.Rectangle> rectangles)
    {
        var result = new Rectangle[rectangles.Count];
        for (int i = 0; i < rectangles.Count; i++)
        {
            result[i] = rectangles[i];
        }

        return result;
    }

    private static Vector3[] Convert(IReadOnlyList<CNA.Vector3> vectors)
    {
        var result = new Vector3[vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            result[i] = vectors[i];
        }

        return result;
    }
}
