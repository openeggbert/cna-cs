using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// The native C ABI cannot expose C# generics directly, so <see cref="Load{T}"/> dispatches by
/// runtime type -- see ../../cnabinding/analysis_binding.md §26. CNA.XnaCompat's
/// <c>ContentManager</c> overrides this same method to additionally recognize its own compat
/// content types, reusing <see cref="LoadNativeTexture2DHandle"/> so it never has to touch
/// CNA.Interop directly (see docs/architecture.md).
/// </summary>
public class ContentManager
{
    private readonly nint _nativeHandleValue;
    private string _rootDirectory = string.Empty;

    /// <summary>
    /// <c>protected internal</c> so CNA.XnaCompat's <c>ContentManager</c> subclass constructor
    /// can forward to it without naming <see cref="CnaHandle"/> -- see docs/architecture.md.
    /// </summary>
    protected internal ContentManager(nint nativeHandleValue)
    {
        _nativeHandleValue = nativeHandleValue;
    }

    public string RootDirectory
    {
        get => _rootDirectory;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_content_set_root_directory(new CnaHandle(_nativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(RootDirectory));
            _rootDirectory = value;
        }
    }

    public virtual T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)new Texture2D(LoadNativeTexture2DHandle(assetName));
        }

        if (typeof(T) == typeof(SpriteFont))
        {
            SpriteFontData data = LoadSpriteFontData(assetName);
            return (T)(object)new SpriteFont(
                new Texture2D(data.TextureHandle),
                data.GlyphBounds,
                data.Cropping,
                data.Characters,
                data.LineSpacing,
                data.Spacing,
                data.Kerning,
                data.DefaultCharacter);
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }

    protected nint LoadNativeTexture2DHandle(string assetName)
    {
        CnaResult result = Native.cna_content_load_texture2d(new CnaHandle(_nativeHandleValue), assetName, out CnaHandle texture);
        CnaException.ThrowIfFailed(result, nameof(Load));
        return texture.Value;
    }

    /// <summary>
    /// The raw pieces of a loaded <c>SpriteFont</c> asset, in exactly the shape
    /// <see cref="Graphics.SpriteFont"/>'s public constructor wants -- returned rather than an
    /// already-built <see cref="Graphics.SpriteFont"/> so <c>CNA.XnaCompat</c>'s
    /// <c>ContentManager</c> can build its own namespace's <c>SpriteFont</c> from the same native
    /// fetch, the same "return raw pieces, let each layer wrap its own type" split
    /// <see cref="LoadNativeTexture2DHandle"/> already uses for <c>Texture2D</c>.
    /// </summary>
    protected readonly record struct SpriteFontData(
        nint TextureHandle,
        IReadOnlyList<Rectangle> GlyphBounds,
        IReadOnlyList<Rectangle> Cropping,
        IReadOnlyList<char> Characters,
        int LineSpacing,
        float Spacing,
        IReadOnlyList<Vector3> Kerning,
        char? DefaultCharacter);

    /// <summary>
    /// No ABI shape for <c>SpriteFont</c> content loading exists upstream -- self-designed for
    /// this repository (see <c>CnaSpriteFontData</c> in CNA.Interop). Deliberately caps a font at
    /// <c>CnaGlyphBuffer.MaxGlyphs</c> (256) glyphs -- generous for XNA's default ASCII-range
    /// content-pipeline output, but a real, documented limitation, not a silent truncation: the
    /// native call is expected to fail with a <see cref="CnaResult"/> error for a font with more
    /// glyphs than that. <see cref="CnaGlyphMetrics.Character"/> crosses the ABI as a full Unicode
    /// code point specifically so it isn't ambiguous for astral-plane characters, but
    /// <see cref="Graphics.SpriteFont"/>'s glyph table is <c>char</c>-keyed (matching real XNA,
    /// which has the same limitation) -- a code point that doesn't fit in one UTF-16 code unit is
    /// rejected here with a clear exception rather than silently truncated into a wrong,
    /// possibly-colliding <c>char</c>.
    /// </summary>
    protected SpriteFontData LoadSpriteFontData(string assetName)
    {
        CnaResult result = Native.cna_content_load_spritefont(new CnaHandle(_nativeHandleValue), assetName, out CnaSpriteFontData native);
        CnaException.ThrowIfFailed(result, nameof(LoadSpriteFontData));

        int glyphCount = native.GlyphCount;
        if (glyphCount < 0 || glyphCount > CnaGlyphBuffer.MaxGlyphs)
        {
            throw new CnaException(
                $"{nameof(LoadSpriteFontData)} received an out-of-range glyph count ({glyphCount}) from the native call " +
                $"-- expected 0..{CnaGlyphBuffer.MaxGlyphs}. This indicates a native/managed ABI mismatch, not a font-content problem.");
        }

        var glyphBounds = new Rectangle[glyphCount];
        var cropping = new Rectangle[glyphCount];
        var characters = new char[glyphCount];
        var kerning = new Vector3[glyphCount];

        for (int i = 0; i < glyphCount; i++)
        {
            CnaGlyphMetrics glyph = native.Glyphs[i];
            glyphBounds[i] = new Rectangle(glyph.Bounds.X, glyph.Bounds.Y, glyph.Bounds.Width, glyph.Bounds.Height);
            cropping[i] = new Rectangle(glyph.Cropping.X, glyph.Cropping.Y, glyph.Cropping.Width, glyph.Cropping.Height);
            characters[i] = ToChar(glyph.Character);
            kerning[i] = new Vector3(glyph.LeftSideBearing, glyph.Width, glyph.RightSideBearing);
        }

        char? defaultCharacter = native.HasDefaultCharacter != 0 ? ToChar(native.DefaultCharacter) : null;

        return new SpriteFontData(
            native.Texture.Value, glyphBounds, cropping, characters, native.LineSpacing, native.Spacing, kerning, defaultCharacter);

        static char ToChar(int codePoint)
        {
            if (codePoint is < 0 or > char.MaxValue || char.IsSurrogate((char)codePoint))
            {
                throw new CnaException(
                    $"SpriteFont glyph code point U+{codePoint:X} does not fit in a single UTF-16 char " +
                    "(SpriteFont's glyph table is char-keyed, matching real XNA's own limitation).");
            }

            return (char)codePoint;
        }
    }
}
