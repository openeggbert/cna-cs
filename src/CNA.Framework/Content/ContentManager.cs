using CNA.Audio;
using CNA.Content.Cnj;
using CNA.Content.Xnb;
using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// The native C ABI cannot expose C# generics directly, so <see cref="Load{T}"/> dispatches by
/// runtime type -- see ../../cnabinding/analysis_binding.md §26. CNA.XnaCompat's
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

    /// <summary>Set by <see cref="Game"/> once its own <see cref="Graphics.GraphicsDevice"/>
    /// becomes available (real XNA content loading generally only ever happens from
    /// <c>LoadContent()</c> onward, by which point this is always set) -- <see langword="null"/>
    /// only before that point, or if this <see cref="ContentManager"/> was hand-built outside the
    /// normal <see cref="Game"/> lifecycle. Only <see cref="Load{T}"/>'s <see cref="Model"/> case
    /// needs this today.</summary>
    public GraphicsDevice? GraphicsDevice { get; set; }

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

        if (typeof(T) == typeof(SoundEffect))
        {
            return (T)(object)new SoundEffect(LoadNativeSoundEffectHandle(assetName));
        }

        if (typeof(T) == typeof(Model))
        {
            return (T)(object)LoadModel(assetName);
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

        object? root = contentReader.ReadRootObjectAndResolveSharedResources();

        if (root is not XnbModelData modelData)
        {
            throw new ContentLoadException(
                $"'{assetName}' is not a Model asset (its .xnb root object's type reader was not ModelReader).");
        }

        return modelData;
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
        CnaResult result = Native.cna_content_load_texture2d(new CnaHandle(_nativeHandleValue), assetName, out CnaHandle texture);
        CnaException.ThrowIfFailed(result, nameof(Load));
        return texture.Value;
    }

    protected nint LoadNativeSoundEffectHandle(string assetName)
    {
        CnaResult result = Native.cna_content_load_soundeffect(new CnaHandle(_nativeHandleValue), assetName, out CnaHandle soundEffect);
        CnaException.ThrowIfFailed(result, nameof(Load));
        return soundEffect.Value;
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
