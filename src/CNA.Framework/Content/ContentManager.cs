using CNA.Audio;
using CNA.Content.Cnj;
using CNA.Content.Xnb;
using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// The native C ABI cannot expose C# generics directly, so <see cref="Load{T}"/> dispatches by
/// runtime type -- see openeggbert/cna's analysis_binding.md §26. CNA.XnaCompat's
/// <c>ContentManager</c> overrides this same method to additionally recognize its own compat
/// content types, reusing <see cref="LoadNativeTexture2DHandle"/> so it never has to touch
/// CNA.Interop directly (see docs/architecture.md).
///
/// <see cref="Load{T}"/>'s <see cref="Model"/> case is genuinely different from every other case
/// here: real XNA's own <c>Content.Load&lt;Model&gt;()</c> reads a compiled <c>.xnb</c> binary
/// asset -- pure C#/BCL logic with no native ABI dependency at all, unlike
/// <see cref="Texture2D"/>/<see cref="SoundEffect"/>/<see cref="SpriteFont"/> (which this project's
/// native CNA engine loads on their behalf). See <c>CNA.Content.Xnb</c>'s own types for the actual
/// <c>.xnb</c> reader (confirmed byte-for-byte against the real openeggbert/cna C++ engine's own
/// reference implementation and a real MonoGame-compiled fixture -- see <c>NEXT.md</c>) -- real,
/// uncompressed <c>.xnb</c> files and real, LZX-compressed <c>.xnb</c> files (<see cref="XnbLzxDecompression"/>/
/// <see cref="LzxDecoder"/>, a direct port of the real C++ engine's own <c>LzxDecoder</c>) are both
/// supported; MonoGame's own Lz4 extension remains out of scope (no local format grounding exists
/// to implement it correctly -- see <see cref="XnbCompression"/>'s own doc comment).
/// <see cref="LoadModel"/> also recognizes a real, minimal-scope subset of the real
/// engine's own <c>.cnj</c> format (<c>CNA.Content.Cnj</c> -- JSON envelope + flat mesh list,
/// <c>BasicEffect</c> only, vertex strides 16/20/24/32 only; see that namespace's own types for the
/// full scope-cut list), tried only when no <c>.xnb</c> file of the same asset name exists, matching
/// the real engine's own dispatch order. Runtime glTF (<c>.gltf</c>/<c>.glb</c>) remains entirely out
/// of scope (see <c>plan.md</c>/<c>NEXT.md</c>). Building the final, real <see cref="Model"/> still needs a real
/// <see cref="Graphics.GraphicsDevice"/> (to construct native-backed <see cref="VertexBuffer"/>/
/// <see cref="IndexBuffer"/> instances), so <see cref="GraphicsDevice"/> below is set by
/// <see cref="Game"/> once its own device becomes available -- <em>that</em> part is native-ABI-blocked,
/// same as the rest of this class's content types.
/// </summary>
public class ContentManager : IDisposable
{
    private readonly nint _nativeHandleValue;

    /// <summary>
    /// <c>protected internal</c> so CNA.XnaCompat's <c>ContentManager</c> subclass constructor
    /// can forward to it without naming <see cref="CnaHandle"/> -- see docs/architecture.md.
    /// </summary>
    protected internal ContentManager(nint nativeHandleValue)
    {
        _nativeHandleValue = nativeHandleValue;
    }

    /// <summary>Set by <see cref="Game"/> once its own <see cref="Graphics.GraphicsDevice"/>
    /// becomes available (real XNA content loading generally only ever happens from
    /// <c>LoadContent()</c> onward, by which point this is always set) -- <see langword="null"/>
    /// only before that point, or if this <see cref="ContentManager"/> was hand-built outside the
    /// normal <see cref="Game"/> lifecycle. Only <see cref="Load{T}"/>'s <see cref="Model"/> case
    /// needs this today.</summary>
    public GraphicsDevice? GraphicsDevice { get; set; }

    /// <summary>
    /// Where assets are loaded from, relative to the title.
    ///
    /// The getter asks native rather than answering from the value last set, which it used to do.
    /// That cache was wrong in one reachable case and right by accident otherwise: a manager whose
    /// root was set by something other than this property -- native's own default, or another
    /// wrapper over the same handle -- reported an empty string.
    /// </summary>
    public unsafe string RootDirectory
    {
        get => NativeStringReader.Read(
            Native.cna_content_manager_get_root_directory_size,
            Native.cna_content_manager_copy_root_directory,
            new CnaHandle(_nativeHandleValue),
            nameof(RootDirectory));
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = CnaStringMarshal.WithStringView(
                value, view => Native.cna_content_manager_set_root_directory(new CnaHandle(_nativeHandleValue), view));
            CnaException.ThrowIfFailed(result, nameof(RootDirectory));
        }
    }

    /// <summary>
    /// The service container assets are resolved through. Matches real XNA's
    /// <c>ServiceProvider</c>.
    ///
    /// Always <see langword="null"/>. <c>content.h</c> documents both manager constructors as
    /// creating one "with a null service provider", and exposes only
    /// <c>cna_content_manager_get_has_service_provider</c> -- a boolean, with no route to obtain the
    /// provider itself. Present rather than omitted so ported XNA source compiles; reporting null is
    /// what the ABI actually says is there.
    /// </summary>
    public IServiceProvider? ServiceProvider => null;

    /// <summary>Unloads every asset, then releases nothing else -- the native manager belongs to
    /// the game that created it. Matches real XNA's <c>ContentManager.Dispose</c>, which is
    /// <c>Unload</c> plus a disposed flag.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>The overridable half, for a subclass that owns more than the base does.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            Unload();
        }
    }

    private bool _disposed;

    /// <summary>Releases every asset this manager loaded. Matches real XNA's <c>Unload</c>: the
    /// manager stays usable and can load again afterwards.</summary>
    public void Unload()
    {
        CnaResult result = Native.cna_content_manager_unload(new CnaHandle(_nativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Unload));
    }

    public virtual T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)new Texture2D(RequireGraphicsDevice<T>(assetName), LoadNativeTexture2DHandle(assetName));
        }

        if (typeof(T) == typeof(SpriteFont))
        {
            SpriteFontData data = LoadSpriteFontData(assetName);
            return (T)(object)new SpriteFont(
                new Texture2D(RequireGraphicsDevice<T>(assetName), data.TextureHandle),
                data.GlyphBounds,
                data.Cropping,
                data.Characters,
                data.LineSpacing,
                data.Spacing,
                data.Kerning,
                data.DefaultCharacter);
        }

        if (typeof(T) == typeof(TextureCube))
        {
            return (T)(object)new TextureCube(RequireGraphicsDevice<T>(assetName), LoadNativeTextureCubeHandle(assetName));
        }

        if (typeof(T) == typeof(SoundEffect))
        {
            return (T)(object)new SoundEffect(LoadNativeSoundEffectHandle(assetName));
        }

        if (typeof(T) == typeof(Model))
        {
            return (T)(object)LoadModel(assetName);
        }

        if (typeof(T) == typeof(Effect))
        {
            return (T)(object)new Effect(RequireGraphicsDevice<T>(assetName), LoadNativeEffectHandle(assetName));
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }

    /// <summary>Parses a real <c>.xnb</c> <see cref="Model"/> asset's bytes (uncompressed or
    /// LZX-compressed) from <see cref="RootDirectory"/> into an intermediate, native-free
    /// <see cref="XnbModelData"/> -- deliberately split out from <see cref="LoadModel"/> (which needs a real
    /// <see cref="Graphics.GraphicsDevice"/> to finish the job) so <c>CNA.XnaCompat</c>'s own
    /// <c>ContentManager</c> can reuse this exact parsing step to build its own compat-typed
    /// <see cref="Model"/>, without duplicating any <c>.xnb</c> format logic -- the same "reuse the
    /// shared low-level parsing/helper, reimplement only the thin native-backed assembly around it"
    /// pattern <c>CNA.XnaCompat.MediaLibrary</c> already established for
    /// <see cref="Media.SavedPictureStore"/>. <c>internal</c>, not <c>protected</c> like
    /// <see cref="LoadNativeTexture2DHandle"/>'s own -- <see cref="XnbModelData"/> is itself
    /// <c>internal</c>, and a <c>protected</c> member's signature must be visible to *any*
    /// subclass in *any* assembly, not just the one (<c>CNA.XnaCompat</c>) this project's own
    /// <c>InternalsVisibleTo</c> grant actually covers (a real <c>CS0050</c> compiler error caught
    /// this during implementation) -- <c>internal</c> matches <see cref="Media.SavedPictureStore"/>'s
    /// own accessibility for the identical reason.</summary>
    internal XnbModelData LoadXnbModelData(string assetName)
    {
        object? root = ReadXnbRootObject(assetName);

        if (root is not XnbModelData modelData)
        {
            throw new ContentLoadException(
                $"'{assetName}' is not a Model asset (its .xnb root object's type reader was not ModelReader).");
        }

        return modelData;
    }

    /// <summary>Parses a real <c>.xnb</c> <c>SpriteFont</c> asset into a native-free
    /// <see cref="XnbSpriteFontData"/>. Same split, and same <c>internal</c> accessibility, as
    /// <see cref="LoadXnbModelData"/> -- see its doc comment for both.</summary>
    internal XnbSpriteFontData LoadXnbSpriteFontData(string assetName)
    {
        object? root = ReadXnbRootObject(assetName);

        if (root is not XnbSpriteFontData spriteFontData)
        {
            throw new ContentLoadException(
                $"'{assetName}' is not a SpriteFont asset (its .xnb root object's type reader was not SpriteFontReader).");
        }

        return spriteFontData;
    }

    /// <summary>Opens a <c>.xnb</c> file, decompresses it when the header says so, and reads its
    /// root object. Extracted when the <c>SpriteFont</c> reader landed: the container half is
    /// identical for every asset type and only the root-object type check differs, so duplicating
    /// it would have meant two copies of the LZX handling below.</summary>
    private object? ReadXnbRootObject(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        string path = ResolveXnbAssetPath(assetName);
        using FileStream stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        XnbHeader header = XnbHeader.Read(reader, stream.Length);

        XnbContentReader contentReader;
        if (header.Compression == XnbCompression.Lzx)
        {
            // A code-review finding caught a real gap here: header.TotalLength is only checked
            // against the actual stream length (in XnbHeader.Read), never against
            // XnbHeader.LzxPayloadOffset -- a file whose header claims exactly 10-13 bytes total
            // (too short to hold the 4-byte decompressed-size field that must follow for an
            // Lzx-flagged file) would previously reach reader.ReadInt32() below with fewer than 4
            // bytes left in the stream, throwing an unhandled System.IO.EndOfStreamException
            // instead of this project's own ContentLoadException contract for corrupt content.
            // Checked here, before that read, rather than after it (where a compressedSize < 0
            // check would be unreachable dead code: reaching it at all already implies
            // TotalLength >= LzxPayloadOffset, since ReadInt32() would have thrown otherwise).
            if (header.TotalLength < XnbHeader.LzxPayloadOffset)
            {
                throw new ContentLoadException(
                    $"'{assetName}' is not a valid LZX-compressed .xnb file (its declared total length is too short to hold a compressed payload).");
            }

            int decompressedSize = reader.ReadInt32();
            int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
            byte[] compressed = reader.ReadBytes(compressedSize);
            byte[] decompressed = XnbLzxDecompression.Decompress(compressed, decompressedSize, assetName);
            contentReader = XnbContentReader.Create(new BinaryReader(new MemoryStream(decompressed)));
        }
        else
        {
            contentReader = XnbContentReader.Create(reader);
        }

        return contentReader.ReadRootObjectAndResolveSharedResources();
    }

    /// <summary>Builds the final, real, native-backed <see cref="Model"/> -- see this type's own doc
    /// comment for why only this assembly step (not the parsing <see cref="LoadXnbModelData"/>/
    /// <see cref="LoadCnjModelData"/> do) is native-ABI-blocked. Dispatch order matches the real
    /// engine's own: a real <c>.xnb</c> file always wins first if one exists for
    /// <paramref name="assetName"/>, only falling back to <c>.cnj</c> when it doesn't -- so a real
    /// <c>.xnb</c> asset always shadows a <c>.cnj</c> file of the same name sitting next to it.
    /// Two of the real engine's own further fallbacks are deliberately <b>not</b> ported: resolving
    /// an <paramref name="assetName"/> that already carries its own extension as-is (a rarely-used
    /// convenience with no precedent anywhere in this project's own <c>.xnb</c>-loading code, which
    /// always appends the extension itself), and runtime glTF (<c>.gltf</c>/<c>.glb</c>, hard out of
    /// scope -- see <c>CNA.Content.Cnj</c>'s own doc comments).</summary>
    /// <summary>
    /// The device every <see cref="GraphicsResource"/>-derived asset now needs, or a
    /// <see cref="ContentLoadException"/> naming the asset if none has been assigned yet.
    ///
    /// Added by Phase 8 WP3, which reparented <see cref="Texture2D"/> onto
    /// <see cref="GraphicsResource"/> and so made a device mandatory where texture loading
    /// previously needed none. Deliberately the same failure shape <see cref="LoadModel"/> has used
    /// since it was written -- a <see cref="ContentLoadException"/> naming the asset, not a bare
    /// <see cref="NullReferenceException"/> from somewhere deeper -- since the cause is identical
    /// (content loaded before <see cref="Game"/> assigns the device).
    /// </summary>
    private GraphicsDevice RequireGraphicsDevice<T>(string assetName) =>
        GraphicsDevice ?? throw new ContentLoadException(
            $"Cannot load {typeof(T).Name} '{assetName}': no GraphicsDevice is available yet (ContentManager.GraphicsDevice is null).");

    protected Model LoadModel(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        if (GraphicsDevice is null)
        {
            throw new ContentLoadException(
                $"Cannot load Model '{assetName}': no GraphicsDevice is available yet (ContentManager.GraphicsDevice is null).");
        }

        if (File.Exists(Path.Combine(RootDirectory, assetName + ".xnb")))
        {
            return XnbModelBuilder.Build(GraphicsDevice, LoadXnbModelData(assetName));
        }

        if (File.Exists(Path.Combine(RootDirectory, assetName + ".cnj")))
        {
            return CnjModelBuilder.Build(GraphicsDevice, LoadCnjModelData(assetName));
        }

        throw new ContentLoadException($"Content file '{assetName}' was not found (tried '{assetName}.xnb' and '{assetName}.cnj').");
    }

    /// <summary>Parses a real, minimal-scope <c>.cnj</c> <see cref="Model"/> asset's JSON (plus its
    /// vertex/index sidecar files) from <see cref="RootDirectory"/> into an intermediate,
    /// native-free <see cref="CnjModelData"/> -- same split, same reuse rationale, and the same
    /// <c>internal</c>-not-<c>protected</c> accessibility reasoning (a real <c>CS0050</c> compiler
    /// error, since <see cref="CnjModelData"/> is itself <c>internal</c>) as
    /// <see cref="LoadXnbModelData"/>'s own doc comment already explains for the <c>.xnb</c> side.</summary>
    internal CnjModelData LoadCnjModelData(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        string path = Path.Combine(RootDirectory, assetName + ".cnj");
        if (!File.Exists(path))
        {
            throw new ContentLoadException($"Content file '{path}' was not found.");
        }

        string json = File.ReadAllText(path);
        return CnjModelReader.Read(json, assetName, RootDirectory);
    }

    private string ResolveXnbAssetPath(string assetName)
    {
        string path = Path.Combine(RootDirectory, assetName + ".xnb");
        if (!File.Exists(path))
        {
            throw new ContentLoadException($"Content file '{path}' was not found.");
        }

        return path;
    }

    protected nint LoadNativeTexture2DHandle(string assetName)
    {
        CnaHandle texture = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            assetName, view => Native.cna_content_manager_load_texture2d(new CnaHandle(_nativeHandleValue), view, out texture));
        CnaException.ThrowIfFailed(result, nameof(Load));
        return texture.AsNint;
    }

    /// <summary>The texture-cube loader. <c>cna_content_manager_load_texture_cube</c> was unbound
    /// until a sweep of unbound header functions found it, so <c>Load&lt;TextureCube&gt;</c> threw
    /// "unsupported content type" for an asset the C API could load all along.</summary>
    protected nint LoadNativeTextureCubeHandle(string assetName)
    {
        CnaHandle texture = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            assetName, view => Native.cna_content_manager_load_texture_cube(new CnaHandle(_nativeHandleValue), view, out texture));
        CnaException.ThrowIfFailed(result, nameof(Load));
        return texture.AsNint;
    }

    protected nint LoadNativeSoundEffectHandle(string assetName)
    {
        CnaHandle soundEffect = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            assetName, view => Native.cna_content_manager_load_sound_effect(new CnaHandle(_nativeHandleValue), view, out soundEffect));
        CnaException.ThrowIfFailed(result, nameof(Load));
        return soundEffect.AsNint;
    }

    /// <summary>
    /// The <c>Load&lt;Effect&gt;</c> route, added once <c>cna_content_manager_load_effect</c>
    /// existed. It reads all three shapes CNA supports -- a compiled <c>.xnb</c> Effect asset, a
    /// <c>.cnj</c> descriptor naming a stock effect, and a <c>.cnj</c> descriptor carrying custom
    /// shader source -- so the caller does not choose between them; the asset does.
    ///
    /// This was the largest single functional blocker on the "will an XNA game run" list: without
    /// it, every ported 3D game with a custom shader stopped at its first
    /// <c>Content.Load&lt;Effect&gt;</c>. <c>internal</c> rather than <c>protected</c> because
    /// Returns a raw <see cref="nint"/>, not a <c>CnaHandle</c>, and is <c>protected</c> like its
    /// three sibling loaders: CNA.XnaCompat has no <c>InternalsVisibleTo</c> grant into CNA.Interop
    /// and so can never name that type -- invariant 5. An <c>internal</c> member returning
    /// <c>CnaHandle</c> compiles here and is unusable from compat, which is how the first draft of
    /// this method failed.
    /// </summary>
    protected nint LoadNativeEffectHandle(string assetName)
    {
        CnaHandle effect = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            assetName, view => Native.cna_content_manager_load_effect(new CnaHandle(_nativeHandleValue), view, out effect));
        CnaException.ThrowIfFailed(result, nameof(Load));
        return effect.AsNint;
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
    /// Parses a <c>SpriteFont</c> asset and uploads its atlas.
    ///
    /// This used to call <c>cna_content_load_spritefont</c>, a P/Invoke that names a function
    /// present in no header -- so every <c>Load&lt;SpriteFont&gt;</c> would have died with an
    /// <c>EntryPointNotFoundException</c>, and the doc comment above it asserted "there is nothing
    /// real to match" while <c>sprite_font.h</c> shipped an eight-function SpriteFont resource. A
    /// header audit found it.
    ///
    /// What the C API genuinely lacks is a font *loader*: <c>content.h</c> has
    /// <c>cna_content_manager_load_texture2d</c>, <c>_load_sound_effect</c> and
    /// <c>_load_texture_cube</c>, and nothing for fonts. So the container is parsed here -- exactly
    /// what this project already does for <c>Model</c> -- and the atlas is uploaded through the
    /// ordinary <see cref="Texture2D"/> path. The 256-glyph cap the old native shape imposed is
    /// gone with it; a real <c>.xnb</c> font is limited only by the file.
    /// </summary>
    protected SpriteFontData LoadSpriteFontData(string assetName)
    {
        XnbSpriteFontData data = LoadXnbSpriteFontData(assetName);
        Texture2D texture = BuildAtlas(assetName, data.Texture);

        // Detached, not read: this method's contract is to hand back a raw handle its caller wraps
        // (so each layer can build its own namespace's Texture2D), and `texture` is a throwaway that
        // existed only to run SetData. Reading NativeHandleValue instead would leave two owners of
        // one handle, and the throwaway's critical finalizer would destroy a texture the SpriteFont
        // is still drawing from -- a use-after-free that only shows up at whatever GC happens to
        // collect it.
        return new SpriteFontData(
            texture.DetachNativeHandle(),
            data.GlyphBounds,
            data.Cropping,
            data.Characters,
            data.LineSpacing,
            data.Spacing,
            data.Kerning,
            data.DefaultCharacter);
    }

    /// <summary>
    /// Uploads a parsed atlas as a real <see cref="Texture2D"/>.
    ///
    /// Only mip level 0 is uploaded. <see cref="Texture2D"/> has no per-level <c>SetData</c>
    /// overload in this binding, and a font atlas is sampled at its native size by
    /// <c>SpriteBatch.DrawString</c>, so the lower levels would never be read. Stated here rather
    /// than silently dropped, because a font compiled with mipmaps is a real input.
    ///
    /// The returned wrapper is a throwaway whose handle the caller detaches -- see
    /// <see cref="LoadSpriteFontData"/>.
    /// </summary>
    private Texture2D BuildAtlas(string assetName, XnbTextureData data)
    {
        GraphicsDevice device = RequireGraphicsDevice<Graphics.SpriteFont>(assetName);

        if (data.Format != SurfaceFormat.Color)
        {
            throw new ContentLoadException(
                $"'{assetName}' has a {data.Format} font atlas. Only {SurfaceFormat.Color} is supported here, " +
                "because that is the only format this binding can upload through Texture2D.SetData.");
        }

        var texture = new Texture2D(device, data.Width, data.Height);
        texture.SetData(data.MipLevels[0]);
        return texture;
    }
}
