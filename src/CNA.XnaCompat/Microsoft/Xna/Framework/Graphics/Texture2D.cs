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
    private readonly int _width;
    private readonly int _height;

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(graphicsDevice, CreateFrameworkTexture(graphicsDevice, width, height, false, SurfaceFormat.Color))
    {
    }

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, CreateFrameworkTexture(graphicsDevice, width, height, mipMap, format))
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.Texture2D(
            (graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice))).Framework,
            nativeHandleValue))
    {
    }

    internal Texture2D(GraphicsDevice graphicsDevice, CNA.Graphics.Texture2D frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
        _width = frameworkTexture.Width;
        _height = frameworkTexture.Height;
    }

    private CNA.Graphics.Texture2D FrameworkTexture2D => (CNA.Graphics.Texture2D)FrameworkTexture;

    public int Width => _width;

    public int Height => _height;

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
        ValidateTransfer(level, rect, data, startIndex, elementCount);
        CNA.Graphics.Texture2D.SetDataFrom(
            NativeHandleValue,
            (CNA.Graphics.SurfaceFormat)(int)Format,
            level,
            rect.ToFramework(),
            data,
            startIndex,
            elementCount);
        GC.KeepAlive(this);
    }

    /// <summary>
    /// Reads texels back through the composed CNA texture while preserving this namespace's own
    /// <see cref="Texture"/> hierarchy and value types.
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
        ValidateTransfer(level, rect, data, startIndex, elementCount);

        CNA.Rectangle? converted = rect is { } r ? new CNA.Rectangle(r.X, r.Y, r.Width, r.Height) : null;

        CNA.Graphics.Texture2D.GetDataInto(
            NativeHandleValue, (CNA.Graphics.SurfaceFormat)(int)Format, level, converted,
            data, startIndex, elementCount);
        GC.KeepAlive(this);
    }

    /// <summary>Matches real XNA's <c>Texture2D.FromStream</c>, re-typed to this namespace's own
    /// <see cref="GraphicsDevice"/> and <see cref="Texture2D"/>. The backend factory builds a
    /// <c>CNA.Graphics.Texture2D</c>, so ownership is transferred into the strict facade through the
    /// same raw-handle constructor <c>ContentManager</c> uses.</summary>
    public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(stream);

        using CNA.Graphics.Texture2D decoded = CNA.Graphics.Texture2D.FromStream(graphicsDevice.Framework, stream);
        return new Texture2D(graphicsDevice, decoded.DetachNativeHandle());
    }

    public static Texture2D FromStream(
        GraphicsDevice graphicsDevice, Stream stream, int width, int height, bool zoom)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(stream);

        using CNA.Graphics.Texture2D decoded =
            CNA.Graphics.Texture2D.FromStream(graphicsDevice.Framework, stream, width, height, zoom);
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

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
    }

    private static CNA.Graphics.Texture2D CreateFrameworkTexture(
        GraphicsDevice graphicsDevice, int width, int height, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ValidateDimensions(width, height);
        if (!Enum.IsDefined(format))
        {
            throw new NotSupportedException($"The surface format value {(int)format} is not supported.");
        }

        return new CNA.Graphics.Texture2D(
            graphicsDevice.Framework, width, height, mipMap, (CNA.Graphics.SurfaceFormat)(int)format);
    }

    private void ValidateTransfer<T>(
        int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if ((uint)level >= (uint)LevelCount)
        {
            throw new InvalidOperationException("The mip level is invalid for this texture.");
        }

        ValidateDataWindow(data.Length, startIndex, elementCount);

        int mipWidth = Math.Max(1, Width >> level);
        int mipHeight = Math.Max(1, Height >> level);
        int transferWidth = mipWidth;
        int transferHeight = mipHeight;
        if (rect is { } region)
        {
            if (region.X < 0 || region.Y < 0 || region.Width <= 0 || region.Height <= 0 ||
                (long)region.X + region.Width > mipWidth ||
                (long)region.Y + region.Height > mipHeight)
            {
                throw new ArgumentException("The rectangle is outside the texture level.", nameof(rect));
            }

            transferWidth = region.Width;
            transferHeight = region.Height;
        }

        ValidateTransferSize<T>(Format, transferWidth, transferHeight, 1, elementCount);
    }

    internal static void ValidateDataWindow(int dataLength, int startIndex, int elementCount)
    {
        if (startIndex < 0 || startIndex > dataLength)
        {
            throw new ArgumentOutOfRangeException("dataIndex");
        }

        if ((long)elementCount + startIndex > dataLength || elementCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elementCount));
        }
    }

    internal static void ValidateTransferSize<T>(
        SurfaceFormat format, int width, int height, int depth, int elementCount)
        where T : struct
    {
        int elementSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        int formatSize = GetFormatSize(format);
        if (elementSize != formatSize && (formatSize <= elementSize || formatSize % elementSize != 0))
        {
            throw new ArgumentException("The element size is incompatible with the surface format.");
        }

        long expectedBytes;
        if (format is SurfaceFormat.Dxt1 or SurfaceFormat.Dxt3 or SurfaceFormat.Dxt5)
        {
            int blockBytes = format == SurfaceFormat.Dxt1 ? 8 : 16;
            expectedBytes = (long)((width + 3) >> 2) * ((height + 3) >> 2) * blockBytes * depth;
        }
        else
        {
            expectedBytes = (long)width * height * depth * formatSize;
        }

        if ((long)elementSize * elementCount != expectedBytes)
        {
            throw new ArgumentException("The element count does not match the requested texture region.");
        }
    }

    private static int GetFormatSize(SurfaceFormat format) => format switch
    {
        SurfaceFormat.Alpha8 => 1,
        SurfaceFormat.Bgr565 or SurfaceFormat.Bgra5551 or SurfaceFormat.Bgra4444 or
            SurfaceFormat.NormalizedByte2 or SurfaceFormat.HalfSingle => 2,
        SurfaceFormat.Color or SurfaceFormat.NormalizedByte4 or SurfaceFormat.Rgba1010102 or
            SurfaceFormat.Rg32 or SurfaceFormat.Single or SurfaceFormat.HalfVector2 => 4,
        SurfaceFormat.Rgba64 or SurfaceFormat.Vector2 or SurfaceFormat.HalfVector4 or
            SurfaceFormat.HdrBlendable => 8,
        SurfaceFormat.Vector4 => 16,
        SurfaceFormat.Dxt1 => 8,
        SurfaceFormat.Dxt3 or SurfaceFormat.Dxt5 => 16,
        _ => throw new NotSupportedException($"The surface format value {(int)format} is not supported."),
    };
}
