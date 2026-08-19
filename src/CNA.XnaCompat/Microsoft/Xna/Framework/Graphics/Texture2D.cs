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
/// <see cref="CNA.Graphics.Texture2D.CreateNativeTexture2DHandle"/>'s doc comment). This is the
/// pattern <see cref="CNA.Graphics.RenderTarget2D"/> established for the identical problem.
/// </summary>
public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, CNA.Graphics.Texture2D.CreateNativeTexture2DHandle(graphicsDevice, width, height))
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    protected internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    protected override void ReleaseNative(nint handleValue) => CNA.Graphics.Texture2D.ReleaseNativeTexture2D(handleValue);

    public virtual int Width => CNA.Graphics.Texture2D.GetTexture2DDimensions(NativeHandleValue).Width;

    public virtual int Height => CNA.Graphics.Texture2D.GetTexture2DDimensions(NativeHandleValue).Height;

    public void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CNA.Graphics.Texture2D.SetDataRgba8(NativeHandleValue, data);
    }

    /// <summary>Converts element-wise before packing -- <see cref="Color"/> here is this
    /// namespace's own type, which converts per element but not array-to-array (see that struct's
    /// own conversion operators).</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var converted = new CNA.Color[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            converted[i] = data[i];
        }

        CNA.Graphics.Texture2D.SetDataRgba8(NativeHandleValue, CNA.Graphics.Texture2D.PackColors(converted));
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
    public void GetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, null, data, 0, data.Length);
    }

    /// <summary>See <see cref="GetData{T}(T[])"/>.</summary>
    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged =>
        GetData(0, null, data, startIndex, elementCount);

    /// <summary>
    /// See <see cref="GetData{T}(T[])"/>. <paramref name="rect"/> is this namespace's own
    /// <see cref="Rectangle"/>, which is why this cannot simply forward.
    ///
    /// <b><see cref="Color"/> is read through a CNA-typed buffer and converted back.</b> The
    /// element-type map lives in CNA.Framework and names CNA's value types; it cannot name this
    /// namespace's duplicates, and invariant 5 says it must not try. So a compat
    /// <c>Color[]</c> would be refused by name -- which is what happened the first time this ran --
    /// even though the two are byte-identical. Converting is the honest fix: the map stays truthful
    /// about what the ABI accepts, and this layer does the translation it exists to do, the same
    /// way <see cref="SetData(Color[])"/> already converts on the way down.
    /// </summary>
    public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);

        CNA.Rectangle? converted = rect is { } r ? new CNA.Rectangle(r.X, r.Y, r.Width, r.Height) : null;

        if (typeof(T) == typeof(Color))
        {
            var native = new CNA.Color[data.Length];
            CNA.Graphics.Texture2D.GetDataInto(
                NativeHandleValue, (CNA.Graphics.SurfaceFormat)(int)Format, level, converted,
                native, startIndex, elementCount);

            Span<T> destination = data;
            for (int i = startIndex; i < startIndex + elementCount; i++)
            {
                Color element = native[i];
                destination[i] = (T)(object)element;
            }

            return;
        }

        CNA.Graphics.Texture2D.GetDataInto(
            NativeHandleValue, (CNA.Graphics.SurfaceFormat)(int)Format, level, converted,
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

    /// <summary>This namespace's own <c>Rectangle</c>. Not a <c>new</c> override: this class
    /// derives from its own namespace's texture base rather than from
    /// <c>CNA.Graphics.Texture2D</c>, so there is nothing to hide.</summary>
    public Rectangle Bounds => new(0, 0, Width, Height);
}
