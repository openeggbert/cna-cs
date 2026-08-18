namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// XNA 4.0-compatible <c>ContentManager</c>. Overrides <c>Load&lt;T&gt;</c> to recognize compat
/// content types (currently <see cref="Graphics.Texture2D"/>), reusing the base class's protected
/// native-load helper so this project never references CNA.Interop directly. See
/// ../../../../../docs/architecture.md and ../../../../../docs/xna-compatibility.md.
///
/// <see cref="Graphics.Model"/> is different from the rest: it reuses the base class's own
/// <c>LoadXnbModelData</c>/<c>LoadCnjModelData</c> (real <c>.xnb</c>/<c>.cnj</c> parsing, no native
/// call at all) directly, then hands the result to <see cref="Graphics.XnbCompatModelBuilder"/>/
/// <see cref="Graphics.CnjCompatModelBuilder"/> -- this compat layer's own counterparts to
/// <c>CNA.Content.Xnb.XnbModelBuilder</c>/<c>CNA.Content.Cnj.CnjModelBuilder</c> -- rather than
/// re-parsing anything. See those types' own doc comments for the full reasoning.
/// </summary>
public class ContentManager : CNA.Content.ContentManager
{
    protected internal ContentManager(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    /// <summary>Re-typed so the compat-typed resources this class constructs (which now require a
    /// device -- see Phase 8 WP3) get a compat-typed one. Holds no field of its own: a pure
    /// pass-through to the base property, the same pattern as
    /// <see cref="Graphics.GraphicsDevice.Indices"/> and for the same desync reason described
    /// there.
    ///
    /// Reads as <see langword="null"/> rather than throwing when the base property holds a
    /// *base-typed* device, exactly as <see cref="LoadCompatModel"/> already does -- see that
    /// method's own doc comment for the code-review finding behind it: <c>GraphicsDevice</c> is a
    /// publicly settable property on the base class, so "non-null but not compat-typed" is
    /// reachable without going through <see cref="Game"/> at all, and must degrade to the same
    /// clean <see cref="ContentLoadException"/> as "null" instead of an unhandled
    /// <see cref="InvalidCastException"/>.</summary>
    public new Graphics.GraphicsDevice? GraphicsDevice
    {
        get => base.GraphicsDevice as Graphics.GraphicsDevice;
        set => base.GraphicsDevice = value;
    }

    /// <summary>Mirrors the base class's own <c>RequireGraphicsDevice</c> (see its doc comment for
    /// why a device became mandatory), re-typed for this namespace. Kept here rather than reusing
    /// the base's <see langword="private"/> one so the failure message names the compat type the
    /// caller actually asked for, and so the not-compat-typed case above is covered too.</summary>
    private Graphics.GraphicsDevice RequireGraphicsDevice<T>(string assetName) =>
        GraphicsDevice ?? throw new ContentLoadException(
            $"Cannot load {typeof(T).Name} '{assetName}': ContentManager.GraphicsDevice is null or not a compat-typed GraphicsDevice.");

    public override T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Graphics.Texture2D))
        {
            return (T)(object)new Graphics.Texture2D(RequireGraphicsDevice<T>(assetName), LoadNativeTexture2DHandle(assetName));
        }

        if (typeof(T) == typeof(Graphics.SpriteFont))
        {
            SpriteFontData data = LoadSpriteFontData(assetName);
            return (T)(object)new Graphics.SpriteFont(
                new Graphics.Texture2D(RequireGraphicsDevice<T>(assetName), data.TextureHandle),
                Convert(data.GlyphBounds),
                Convert(data.Cropping),
                data.Characters,
                data.LineSpacing,
                data.Spacing,
                Convert(data.Kerning),
                data.DefaultCharacter);
        }

        if (typeof(T) == typeof(Audio.SoundEffect))
        {
            return (T)(object)new Audio.SoundEffect(LoadNativeSoundEffectHandle(assetName));
        }

        if (typeof(T) == typeof(Graphics.Model))
        {
            return (T)(object)LoadCompatModel(assetName);
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }

    /// <summary>Same <c>GraphicsDevice</c>-availability contract as the base class's own
    /// <c>LoadModel</c>. <c>GraphicsDevice</c> is guaranteed compat-typed for every *normally*
    /// reachable compat <see cref="ContentManager"/> instance (see <c>Microsoft.Xna.Framework.Game</c>'s
    /// own doc comment: its <c>EnsureGraphicsDevice</c> always sets <c>Content.GraphicsDevice</c>
    /// from its own covariant-return <c>CreateGraphicsDevice</c> hook) -- but unlike this session's
    /// other "single construction seam" downcasts, <c>GraphicsDevice</c> is a <em>publicly
    /// settable</em> property on the base class, not something only ever assigned through one
    /// controlled path: a code-review finding correctly pointed out that a base-typed
    /// <c>CNA.Graphics.GraphicsDevice</c> assigned to it directly (bypassing <c>Game</c> entirely)
    /// would previously have thrown an unhandled <see cref="InvalidCastException"/> instead of a
    /// clean, documented <see cref="CNA.Content.ContentLoadException"/> -- fixed below with a
    /// pattern-match that treats "non-null but not compat-typed" the same as "null."</summary>
    private Graphics.Model LoadCompatModel(string assetName)
    {
        if (base.GraphicsDevice is not Graphics.GraphicsDevice graphicsDevice)
        {
            throw new CNA.Content.ContentLoadException(
                $"Cannot load Model '{assetName}': ContentManager.GraphicsDevice is null or not a compat-typed GraphicsDevice.");
        }

        // Same .xnb-then-.cnj dispatch order as the base class's own LoadModel -- a real .xnb file
        // always shadows a .cnj of the same asset name. Each branch's File.Exists check here is
        // followed by LoadXnbModelData/LoadCnjModelData re-resolving and re-checking the identical
        // path -- a code-review finding flagged the redundant stat() this costs, but the base
        // class's own LoadModel already made this exact trade-off for the .xnb case (see its own
        // doc comment), so this only extends an already-accepted pattern rather than introduce a
        // new one; not worth restructuring already-reviewed-clean load-helper signatures to shave
        // one syscall off a content-loading path that isn't a hot loop.
        if (File.Exists(Path.Combine(RootDirectory, assetName + ".xnb")))
        {
            return Graphics.XnbCompatModelBuilder.Build(graphicsDevice, LoadXnbModelData(assetName));
        }

        if (File.Exists(Path.Combine(RootDirectory, assetName + ".cnj")))
        {
            return Graphics.CnjCompatModelBuilder.Build(graphicsDevice, LoadCnjModelData(assetName));
        }

        throw new CNA.Content.ContentLoadException($"Content file '{assetName}' was not found (tried '{assetName}.xnb' and '{assetName}.cnj').");
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
