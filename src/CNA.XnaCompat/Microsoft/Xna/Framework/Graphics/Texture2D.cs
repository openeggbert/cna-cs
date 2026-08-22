namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>Texture2D</c>.
///
/// Derives from this namespace's own <see cref="Texture"/> rather than from
/// <see cref="CNA.Graphics.Texture2D"/>, so that real XNA's own
/// <c>Texture2D : Texture : GraphicsResource</c> chain holds here too and game code's
/// <c>Texture t = someTexture2D;</c> (or a <c>GraphicsDevice.Textures[0] = someTexture2D;</c>
/// binding, whose collection is typed on <see cref="Texture"/>) compiles. C# has single
/// inheritance, so that ancestry rules out also inheriting <see cref="CNA.Graphics.Texture2D"/>'s
/// implementation -- exactly the constraint <see cref="RenderTarget2D"/> already documents on the
/// other side of the same fork.
///
/// No logic is duplicated to pay for it: every member below is a one-line call into the same
/// <c>internal static</c> helpers on <see cref="CNA.Graphics.Texture2D"/> that its own instance
/// members call, which exist for precisely this purpose (see
/// <c>CNA.Graphics.Texture2D.CreateNativeTexture2DHandle</c>'s doc comment). This is the
/// pattern <see cref="CNA.Graphics.RenderTarget2D"/> established for the identical problem.
/// </summary>
public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(graphicsDevice, new CNA.Graphics.Texture2D(graphicsDevice, width, height))
    {
    }

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, new CNA.Graphics.Texture2D(
            graphicsDevice, width, height, mipMap, (CNA.Graphics.SurfaceFormat)(int)format))
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.Texture2D(graphicsDevice, nativeHandleValue))
    {
    }

    internal Texture2D(GraphicsDevice graphicsDevice, CNA.Graphics.Texture2D frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
    }

    private CNA.Graphics.Texture2D FrameworkTexture2D => (CNA.Graphics.Texture2D)FrameworkTexture;

    public int Width => FrameworkTexture2D.Width;

    public int Height => FrameworkTexture2D.Height;

    protected override void Dispose(bool arg0)
    {
        if (!IsDisposed)
        {
            DisposeFrameworkTexture();
        }

        base.Dispose(arg0);
    }

    public void SetData<T>(T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, null, data, 0, data.Length);
    }

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        SetData(0, null, data, startIndex, elementCount);

    public void SetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        CNA.Graphics.Texture2D.SetDataFrom(
            NativeHandleValue,
            CompatTextureDataType.Of<T>(),
            level,
            rect is { } region ? (CNA.Rectangle)region : null,
            data,
            startIndex,
            elementCount);
    }

    /// <summary>
    /// Reads texels back. Re-typed rather than inherited, because this type derives from *this*
    /// namespace's <see cref="Texture"/> and not from <c>CNA.Graphics.Texture2D</c>.
    ///
    /// <b>Absent until the compat layer was first run against the library.</b> GetData was
    /// implemented on the CNA layer and never offered here, so a ported game -- which uses this
    /// type -- could not read a texture at all. Nothing caught it: the type-level diff sees
    /// <c>Texture2D</c> on both sides, and every integration test used the CNA type.
    /// </summary>
    public void GetData<T>(T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, null, data, 0, data.Length);
    }

    /// <summary>See <see cref="GetData{T}(T[])"/>.</summary>
    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        GetData(0, null, data, startIndex, elementCount);

    /// <summary>
    /// See <see cref="GetData{T}(T[])"/>. <paramref name="rect"/> is this namespace's own
    /// <see cref="Rectangle"/>, which is why this cannot simply forward.
    ///
    /// The compat mapper selects the matching native transfer tag while the pinned struct array is
    /// copied directly. Types containing managed references are rejected before native is called.
    /// </summary>
    public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);

        CNA.Rectangle? converted = rect is { } r ? new CNA.Rectangle(r.X, r.Y, r.Width, r.Height) : null;

        CNA.Graphics.Texture2D.GetDataInto(
            NativeHandleValue, (CNA.Graphics.SurfaceFormat)(int)Format,
            CompatTextureDataType.Of<T>(), level, converted,
            data, startIndex, elementCount);
    }

    /// <summary>Matches real XNA's <c>Texture2D.FromStream</c>, re-typed to this namespace's own
    /// <see cref="GraphicsDevice"/> and <see cref="Texture2D"/>. Cannot forward to the base
    /// factory: that one builds a <c>CNA.Graphics.Texture2D</c>, and a compat texture is a separate
    /// class -- so it goes through the same <c>protected internal</c> raw-handle constructor
    /// <c>ContentManager</c> uses.</summary>
    public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(stream);

        using CNA.Graphics.Texture2D decoded = CNA.Graphics.Texture2D.FromStream(graphicsDevice, stream);
        return new Texture2D(graphicsDevice, decoded.DetachNativeHandle());
    }

    public static Texture2D FromStream(
        GraphicsDevice graphicsDevice, Stream stream, int width, int height, bool zoom)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(stream);

        using CNA.Graphics.Texture2D decoded =
            CNA.Graphics.Texture2D.FromStream(graphicsDevice, stream, width, height, zoom);
        return new Texture2D(graphicsDevice, decoded.DetachNativeHandle());
    }

    public void SaveAsPng(Stream stream, int width, int height) =>
        FrameworkTexture2D.SaveAsPng(stream, width, height);

    public void SaveAsJpeg(Stream stream, int width, int height) =>
        FrameworkTexture2D.SaveAsJpeg(stream, width, height);

    /// <summary>This namespace's own <c>Rectangle</c>. Not a <c>new</c> override: this class
    /// derives from its own namespace's texture base rather than from
    /// <c>CNA.Graphics.Texture2D</c>, so there is nothing to hide.</summary>
    public Rectangle Bounds => new(0, 0, Width, Height);
}
