using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// A decoded CNB sprite font: the glyph table and the atlas that goes with it.
///
/// <b>Why this is a decoded description rather than a <see cref="SpriteFont"/>.</b> A
/// <see cref="SpriteFont"/> holds a live device texture, and a decoded file does not have one --
/// the same split the CNB texture and model slices make. <see cref="CnbSpriteFontLoader"/> is the
/// step that needs a device.
///
/// <b>Ownership.</b> The font handle is owned and destroyed here. The atlas is a *copy* CNA hands
/// out rather than a view into the font -- the route is named <c>copy_atlas</c> and returns a new
/// texture description the caller releases -- so <see cref="CopyAtlas"/> gives the caller its own
/// <see cref="CnbTexture"/> to dispose, and a font disposed first cannot invalidate it.
/// </summary>
public sealed class CnbSpriteFont : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly CnaCnbSpriteFontInfo _info;

    private CnbSpriteFont(nint handleValue, CnaCnbSpriteFontInfo info)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_sprite_font_data_destroy(new CnaHandle(h)).IsSuccess());
        _info = info;
    }

    /// <summary>Decodes the sprite font a container holds.</summary>
    /// <exception cref="CnaException">The document is not a sprite font, or its declared counts and
    /// payload lengths disagree.</exception>
    public static CnbSpriteFont Decode(CnbDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CnaResult result = Native.cna_cnb_decode_sprite_font(document.NativeHandle, out CnaHandle font);
        CnaException.ThrowIfFailed(result, nameof(Decode));
        GC.KeepAlive(document);

        var info = CnaCnbSpriteFontInfo.Versioned();
        CnaResult infoResult = Native.cna_cnb_sprite_font_data_get_info(font, ref info);
        if (!infoResult.IsSuccess())
        {
            // Ours the moment the decode succeeded, so it is released even though construction
            // never completed -- the shape CnbTexture.Adopt established.
            _ = Native.cna_cnb_sprite_font_data_destroy(font);
            CnaException.ThrowIfFailed(infoResult, nameof(Decode));
        }

        return new CnbSpriteFont(font.AsNint, info);
    }

    /// <summary>Opens a <c>.cnb</c> file and decodes the sprite font in it.</summary>
    public static CnbSpriteFont DecodeFile(string path)
    {
        using CnbDocument document = CnbDocument.Open(path);
        return Decode(document);
    }

    /// <summary>How many glyphs the font carries.</summary>
    public int GlyphCount => checked((int)_info.GlyphCount);

    /// <summary>Vertical distance between two lines of text, in pixels.</summary>
    public int LineSpacing => _info.LineSpacing;

    /// <summary>Extra horizontal space inserted between glyphs.</summary>
    public float Spacing => _info.Spacing;

    /// <summary>
    /// The character substituted for one the font has no glyph for, or <see langword="null"/> when
    /// the font declares none -- in which case XNA throws rather than substituting.
    ///
    /// Nullable rather than defaulting to <c>'\0'</c>, because CNA carries the presence flag
    /// separately: a font without a default and a font whose default is U+0000 are the same two
    /// bytes, and collapsing them would turn "throw on a missing glyph" into "draw a null".
    /// </summary>
    public char? DefaultCharacter =>
        _info.HasDefaultCharacter != 0 ? (char)_info.DefaultCharacter : null;

    /// <summary>One glyph's character, bounds, cropping and kerning.</summary>
    public CnbGlyph GetGlyph(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var glyph = CnaSpriteFontGlyph.Versioned();
        CnaResult result = Native.cna_cnb_sprite_font_data_get_glyph(Handle, (ulong)index, ref glyph);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetGlyph));

        return new CnbGlyph(
            (char)glyph.Character,
            new Rectangle(
                glyph.GlyphBounds.X, glyph.GlyphBounds.Y, glyph.GlyphBounds.Width, glyph.GlyphBounds.Height),
            new Rectangle(
                glyph.Cropping.X, glyph.Cropping.Y, glyph.Cropping.Width, glyph.Cropping.Height),
            new Vector3(glyph.Kerning.X, glyph.Kerning.Y, glyph.Kerning.Z));
    }

    /// <summary>
    /// The font's atlas, as its own <see cref="CnbTexture"/> the caller disposes.
    ///
    /// A copy rather than a borrow, which is CNA's choice and not this binding's: the route is
    /// <c>copy_atlas</c> and its output is documented as a new description. That is why disposing
    /// this font does not invalidate an atlas already taken from it.
    /// </summary>
    public CnbTexture CopyAtlas()
    {
        CnaResult result = Native.cna_cnb_sprite_font_data_copy_atlas(Handle, out CnaHandle atlas);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(CopyAtlas));
        return CnbTexture.Adopt(atlas.AsNint, nameof(CopyAtlas));
    }

    public void Dispose() => _handle.Dispose();

    internal CnaHandle Handle => new(_handle.DangerousGetHandle());
}

/// <summary>One glyph of a <see cref="CnbSpriteFont"/>. A snapshot, not a view.</summary>
public readonly struct CnbGlyph
{
    internal CnbGlyph(char character, Rectangle bounds, Rectangle cropping, Vector3 kerning)
    {
        Character = character;
        Bounds = bounds;
        Cropping = cropping;
        Kerning = kerning;
    }

    /// <summary>The character this glyph draws.</summary>
    public char Character { get; }

    /// <summary>Where the glyph sits in the atlas.</summary>
    public Rectangle Bounds { get; }

    /// <summary>The glyph's own offset and advance box within its cell.</summary>
    public Rectangle Cropping { get; }

    /// <summary>Left bearing, width and right bearing, as XNA packs them into a
    /// <see cref="Vector3"/>.</summary>
    public Vector3 Kerning { get; }
}

/// <summary>
/// Turns a decoded <see cref="CnbSpriteFont"/> into a drawable <see cref="SpriteFont"/>.
///
/// Separate from <see cref="CnbSpriteFont"/> for the same reason <see cref="CnbTextureLoader"/> is
/// separate from <see cref="CnbTexture"/>: this is the step that needs a graphics device, and a
/// decoded description should be readable without one.
/// </summary>
public static class CnbSpriteFontLoader
{
    /// <summary>Uploads the atlas and builds the font.</summary>
    public static SpriteFont Upload(GraphicsDevice graphicsDevice, CnbSpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(font);

        using CnbTexture atlas = font.CopyAtlas();
        Texture2D texture = CnbTextureLoader.Upload(graphicsDevice, atlas);

        var bounds = new Rectangle[font.GlyphCount];
        var cropping = new Rectangle[font.GlyphCount];
        var characters = new char[font.GlyphCount];
        var kerning = new Vector3[font.GlyphCount];

        for (int index = 0; index < font.GlyphCount; index++)
        {
            CnbGlyph glyph = font.GetGlyph(index);
            bounds[index] = glyph.Bounds;
            cropping[index] = glyph.Cropping;
            characters[index] = glyph.Character;
            kerning[index] = glyph.Kerning;
        }

        return new SpriteFont(
            texture, bounds, cropping, characters, font.LineSpacing, font.Spacing, kerning,
            font.DefaultCharacter);
    }

    /// <summary>Opens a <c>.cnb</c> file and builds the font it holds.</summary>
    public static SpriteFont LoadSpriteFont(GraphicsDevice graphicsDevice, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using CnbSpriteFont font = CnbSpriteFont.DecodeFile(path);
        return Upload(graphicsDevice, font);
    }
}
