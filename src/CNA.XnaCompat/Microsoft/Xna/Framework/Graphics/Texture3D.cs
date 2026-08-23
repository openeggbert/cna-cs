namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture3D</c>. Its public ancestry follows XNA's
/// <c>Texture3D : Texture : GraphicsResource</c>; one internal <see cref="CNA.Graphics.Texture3D"/>
/// owns and services the native resource.</summary>
public class Texture3D : Texture
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _depth;

    /// <summary>Wraps an already-created native handle -- the landing point for a texture read back
    /// out of a shader parameter. <c>protected internal</c> so <see cref="EffectParameter"/> can
    /// reach it, matching <see cref="Texture2D"/>'s own raw-handle constructor.</summary>
    internal Texture3D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.Texture3D(
            (graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice))).Framework,
            nativeHandleValue))
    {
    }

    internal Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : this(graphicsDevice, new CNA.Graphics.Texture3D(
            (graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice))).Framework,
            width, height, depth))
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, CreateFrameworkTexture(graphicsDevice, width, height, depth, mipMap, format))
    {
    }

    private Texture3D(GraphicsDevice graphicsDevice, CNA.Graphics.Texture3D frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
        _width = frameworkTexture.Width;
        _height = frameworkTexture.Height;
        _depth = frameworkTexture.Depth;
    }

    private CNA.Graphics.Texture3D FrameworkTexture3D => (CNA.Graphics.Texture3D)FrameworkTexture;

    public int Width => _width;

    public int Height => _height;

    public int Depth => _depth;

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
        SetData(0, 0, 0, Width, Height, 0, Depth, data, 0, data.Length);
    }

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, 0, 0, Width, Height, 0, Depth, data, startIndex, elementCount);
    }

    public void SetData<T>(
        int level, int left, int top, int right, int bottom, int front, int back,
        T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ValidateTransfer(level, left, top, right, bottom, front, back, data, startIndex, elementCount);
        if (typeof(T) == typeof(Color))
        {
            FrameworkTexture3D.SetData(
                level, left, top, right, bottom, front, back,
                ConvertColors(data), startIndex, elementCount);
            return;
        }

        FrameworkTexture3D.SetDataBytes(
            level, left, top, right, bottom, front, back,
            data, startIndex, elementCount);
    }

    public void GetData<T>(T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, 0, 0, Width, Height, 0, Depth, data, 0, data.Length);
    }

    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, 0, 0, Width, Height, 0, Depth, data, startIndex, elementCount);
    }

    public void GetData<T>(
        int level, int left, int top, int right, int bottom, int front, int back,
        T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ValidateTransfer(level, left, top, right, bottom, front, back, data, startIndex, elementCount);
        RequireColorElement<T>();

        CNA.Color[] values = FrameworkTexture3D.GetData(
            level, left, top, right, bottom, front, back);
        if (values.Length > elementCount)
        {
            throw new ArgumentException("The destination window is too small for the requested volume.", nameof(elementCount));
        }

        for (int i = 0; i < values.Length; i++)
        {
            data[startIndex + i] = (T)(object)values[i].ToCompat();
        }
    }

    private static CNA.Color[] ConvertColors<T>(T[] source) where T : struct
    {
        RequireColorElement<T>();
        var result = new CNA.Color[source.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ((Color)(object)source[i]).ToFramework();
        }

        return result;
    }

    private static void RequireColorElement<T>() where T : struct
    {
        if (typeof(T) != typeof(Color))
        {
            throw new NotSupportedException(
                $"CNA's current Texture3D C ABI has no raw readback route; {typeof(T)} cannot yet be used with GetData<T>.");
        }
    }

    private static CNA.Graphics.Texture3D CreateFrameworkTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        int depth,
        bool mipMap,
        SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (!Enum.IsDefined(format))
        {
            throw new NotSupportedException($"The surface format value {(int)format} is not supported.");
        }

        return new CNA.Graphics.Texture3D(
            graphicsDevice.Framework, width, height, depth, mipMap,
            (CNA.Graphics.SurfaceFormat)(int)format);
    }

    private void ValidateTransfer<T>(
        int level,
        int left,
        int top,
        int right,
        int bottom,
        int front,
        int back,
        T[] data,
        int startIndex,
        int elementCount)
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

        Texture2D.ValidateDataWindow(data.Length, startIndex, elementCount);

        int mipWidth = Math.Max(1, Width >> level);
        int mipHeight = Math.Max(1, Height >> level);
        int mipDepth = Math.Max(1, Depth >> level);
        if (left < 0 || top < 0 || front < 0 || right <= left || bottom <= top || back <= front ||
            right > mipWidth || bottom > mipHeight || back > mipDepth)
        {
            throw new ArgumentException("The box is outside the texture level.", "box");
        }

        Texture2D.ValidateTransferSize<T>(
            Format, right - left, bottom - top, back - front, elementCount);
    }
}
