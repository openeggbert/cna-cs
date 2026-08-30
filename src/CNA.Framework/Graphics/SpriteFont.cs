using CNA.Interop;
namespace CNA.Graphics;

/// <summary>
/// A bitmap font: a <see cref="Texture2D"/> atlas plus a per-character glyph table. Real XNA 4.0
/// exposes a public constructor taking raw glyph arrays (for third-party font-building tools, not
/// just its content pipeline), and that constructor is reproduced here field-for-field. That makes
/// <see cref="MeasureString(string)"/> pure managed code, fully real and testable today, same as
/// the math value types. <c>SpriteBatch.DrawString</c> goes through the native-backed <c>Draw</c>
/// primitive once per glyph, so it inherits <c>SpriteBatch</c>'s status.
///
/// <b>Deliberately managed, and this needed correcting.</b> The doc comment here used to claim this
/// type "needs no new native ABI surface at all"; a header audit found <c>sprite_font.h</c>, an
/// eight-function SpriteFont resource that has been shipping all along
/// (<c>cna_sprite_font_create</c> from a glyph table, <c>_get_info</c>, <c>_copy_characters</c>,
/// the three setters, <c>_measure_utf8</c>, <c>_destroy</c>).
///
/// It is still managed, but <b>the reason recorded here was wrong and is corrected</b>. This used to
/// say the native resource "exposes no per-glyph readback -- no bounds, no cropping, no kerning", so
/// a native-owned font "could be measured and never drawn". <c>cna_sprite_font_copy_glyphs</c>
/// returns exactly those three things, its own header explains that it exists *because* measuring is
/// not drawing, and <c>ContentManager.LoadSpriteFontData</c> in this repository has been calling it
/// all along -- it loads a native font, reads the glyph table back, and destroys the font. The
/// stated blocker was contradicted by the binding's own code.
///
/// What is actually true is narrower: nothing here *retains* a native font. The load path destroys
/// it once the table has been copied out, and the public constructor never creates one, so there is
/// no native font handle to hand to <c>cna_sprite_batch_draw_string</c>. Adopting that route means
/// giving this type a native handle and a lifetime, which is a real change and not a blocked one --
/// see plan.md A1, where it is measured rather than assumed.
///
/// <c>ContentManager.Load&lt;SpriteFont&gt;</c> parses the <c>.xnb</c> container managed-side, the
/// same as <c>Model</c> -- see <c>ContentManager.LoadSpriteFontData</c> for why, and for the
/// fabricated P/Invoke it replaced.
/// </summary>
public class SpriteFont
{
    private readonly char[] _characters;
    private readonly Rectangle[] _glyphBounds;
    private readonly Rectangle[] _cropping;
    private readonly Vector3[] _kerning;
    private readonly Dictionary<char, int> _characterIndex;
    private NativeResourceHandle? _nativeFont;
    private bool _nativeFontUnavailable;

    public SpriteFont(
        Texture texture,
        IReadOnlyList<Rectangle> glyphBounds,
        IReadOnlyList<Rectangle> cropping,
        IReadOnlyList<char> characters,
        int lineSpacing,
        float spacing,
        IReadOnlyList<Vector3> kerning,
        char? defaultCharacter)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(glyphBounds);
        ArgumentNullException.ThrowIfNull(cropping);
        ArgumentNullException.ThrowIfNull(characters);
        ArgumentNullException.ThrowIfNull(kerning);

        if (glyphBounds.Count != characters.Count || cropping.Count != characters.Count || kerning.Count != characters.Count)
        {
            throw new ArgumentException(
                $"{nameof(glyphBounds)}, {nameof(cropping)}, {nameof(characters)}, and {nameof(kerning)} must all have the same length.");
        }

        Texture = texture;
        LineSpacing = lineSpacing;
        Spacing = spacing;
        DefaultCharacter = defaultCharacter;

        _characters = [.. characters];
        _glyphBounds = [.. glyphBounds];
        _cropping = [.. cropping];
        _kerning = [.. kerning];

        _characterIndex = new Dictionary<char, int>(_characters.Length);
        for (int i = 0; i < _characters.Length; i++)
        {
            _characterIndex[_characters[i]] = i;
        }
    }

    public Texture Texture { get; }

    public IReadOnlyList<char> Characters => _characters;

    public int LineSpacing { get; set; }

    public float Spacing { get; set; }

    public char? DefaultCharacter { get; set; }

    public Vector2 MeasureString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Walk(text, null);
    }

    /// <summary>Per-glyph draw data for <c>SpriteBatch.DrawString</c>: <c>Anchor</c> is
    /// the point (in unscaled text-space pixels, relative to the string's own origin) that this
    /// glyph's own <c>Draw</c> origin should be offset by, so that drawing every glyph as its own
    /// sprite with <c>origin - Anchor</c> reproduces the whole string rotating/scaling as one
    /// rigid body around the caller's <c>origin</c>/<c>position</c>.</summary>
    internal readonly struct GlyphPlacement(Rectangle sourceRectangle, Vector2 anchor)
    {
        public readonly Rectangle SourceRectangle = sourceRectangle;
        public readonly Vector2 Anchor = anchor;
    }

    internal void AppendGlyphPlacements(string text, List<GlyphPlacement> placements) => Walk(text, placements);

    /// <summary>
    /// This font's native counterpart, created on first use and kept for the font's lifetime, or
    /// zero when this build cannot make one.
    ///
    /// <b>Lazy, not eager.</b> The public constructor is XNA's -- third-party font-building tools
    /// use it -- and it is pure managed code today, as is <see cref="MeasureString(string)"/>.
    /// Creating a native font in the constructor would make constructing a font require a live
    /// game, which would be a real behaviour change for measuring code that never draws.
    ///
    /// <b>Rebuilt rather than retained from the load path.</b> <c>ContentManager</c> destroys the
    /// font it loads as soon as it has copied the glyph table out, deliberately and unconditionally,
    /// so the atlas can never be held hostage by a failure on the way out. Keeping that handle alive
    /// instead would mean unpicking that ordering for every load; rebuilding here is lossless,
    /// because <c>cna_sprite_font_copy_glyphs</c> is documented as the exact inverse of
    /// <c>cna_sprite_font_create</c>, and it makes the loaded and hand-built cases identical.
    /// </summary>
    internal nint NativeFontHandleValue
    {
        get
        {
            if (_nativeFont is not null)
            {
                return _nativeFont.DangerousGetHandle();
            }

            if (_nativeFontUnavailable)
            {
                return 0;
            }

            try
            {
                nint created = CreateNativeFontHandle();
                _nativeFont = new NativeResourceHandle(created, DestroyNative);
                return created;
            }
            catch (CnaException)
            {
                // A build without the native SpriteFont resource answers here. Drawing falls back to
                // the per-glyph path, which needs nothing native beyond the atlas, so this is a
                // slower font rather than an unusable one -- and asking again every frame would make
                // it a slower font that also allocates an exception every frame.
                _nativeFontUnavailable = true;
                return 0;
            }
        }
    }

    private static bool DestroyNative(nint handleValue) =>
        Native.cna_sprite_font_destroy(new CnaHandle(handleValue)).IsSuccess();

    /// <summary>
    /// Builds a native SpriteFont from this font's own glyph table, and hands back its handle.
    ///
    /// <see cref="NativeFontHandleValue"/> owns what this returns and is the route drawing uses;
    /// this is separated out only so the creation and the ownership read as two things.
    /// </summary>
    internal unsafe nint CreateNativeFontHandle()
    {
        var glyphs = new CnaSpriteFontGlyph[_characters.Length];
        for (int i = 0; i < glyphs.Length; i++)
        {
            glyphs[i] = CnaSpriteFontGlyph.Versioned();
            glyphs[i].GlyphBounds = new CnaRectangle(
                _glyphBounds[i].X, _glyphBounds[i].Y, _glyphBounds[i].Width, _glyphBounds[i].Height);
            glyphs[i].Cropping = new CnaRectangle(
                _cropping[i].X, _cropping[i].Y, _cropping[i].Width, _cropping[i].Height);
            glyphs[i].Character = _characters[i];
            glyphs[i].Kerning = _kerning[i].ToNative();
        }

        fixed (CnaSpriteFontGlyph* first = glyphs)
        {
            CnaSpriteFontCreateInfo createInfo = CnaSpriteFontCreateInfo.Versioned();
            createInfo.Texture = new CnaHandle(Texture.NativeHandleValue);
            createInfo.Glyphs = first;
            createInfo.GlyphCount = (ulong)glyphs.Length;
            createInfo.LineSpacing = LineSpacing;
            createInfo.Spacing = Spacing;
            createInfo.DefaultCharacter = DefaultCharacter ?? '\0';
            createInfo.HasDefaultCharacter = (byte)(DefaultCharacter.HasValue ? 1 : 0);

            CnaResult result = Native.cna_sprite_font_create(in createInfo, out CnaHandle font);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(CreateNativeFontHandle));
            return font.AsNint;
        }
    }


    private int ResolveIndex(char c)
    {
        if (_characterIndex.TryGetValue(c, out int index))
        {
            return index;
        }

        if (DefaultCharacter is char fallback && _characterIndex.TryGetValue(fallback, out int fallbackIndex))
        {
            return fallbackIndex;
        }

        throw new ArgumentException(
            $"Character '{c}' is not in this SpriteFont, and no {nameof(DefaultCharacter)} is set.");
    }

    /// <summary>
    /// The single walk both <see cref="MeasureString(string)"/> and
    /// <see cref="AppendGlyphPlacements"/> are built on, so the two can never silently disagree.
    /// Follows the standard "ABC" kerning-triple (left-side bearing / character width /
    /// right-side bearing, packed into <see cref="Vector3"/> as X/Y/Z) plus cropping-rectangle
    /// layout algorithm XNA/MonoGame both use for bitmap fonts -- not invented for this
    /// repository, but also not verified byte-for-byte against a real XNA binary (none is
    /// available in this environment); see NEXT.md. Known incompleteness: does not implement
    /// XNA's <c>SpriteEffects</c>-driven line/character reversal for flipped text -- flip
    /// effects on <c>DrawString</c> currently just flip each glyph sprite in place, not the
    /// string's reading order.
    /// </summary>
    private Vector2 Walk(string text, List<GlyphPlacement>? placements)
    {
        if (text.Length == 0)
        {
            return Vector2.Zero;
        }

        float width = 0f;
        float finalLineHeight = LineSpacing;
        var offset = Vector2.Zero;
        bool firstGlyphOfLine = true;

        foreach (char c in text)
        {
            if (c == '\r')
            {
                continue;
            }

            if (c == '\n')
            {
                finalLineHeight = LineSpacing;
                offset.X = 0f;
                offset.Y += LineSpacing;
                firstGlyphOfLine = true;
                continue;
            }

            int index = ResolveIndex(c);
            Vector3 kerning = _kerning[index];
            Rectangle cropping = _cropping[index];
            float leftSideBearing = kerning.X;
            float glyphWidth = kerning.Y;
            float rightSideBearing = kerning.Z;

            if (firstGlyphOfLine)
            {
                offset.X += MathF.Max(leftSideBearing, 0f);
                firstGlyphOfLine = false;
            }
            else
            {
                offset.X += Spacing + leftSideBearing;
            }

            placements?.Add(new GlyphPlacement(_glyphBounds[index], new Vector2(offset.X + cropping.X, offset.Y + cropping.Y)));

            offset.X += glyphWidth;

            float proposedWidth = offset.X + MathF.Max(rightSideBearing, 0f);
            if (proposedWidth > width)
            {
                width = proposedWidth;
            }

            offset.X += rightSideBearing;

            if (cropping.Height > finalLineHeight)
            {
                finalLineHeight = cropping.Height;
            }
        }

        return new Vector2(width, offset.Y + finalLineHeight);
    }
}
