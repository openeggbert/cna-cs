namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture3D</c>. Its public ancestry follows XNA's
/// <c>Texture3D : Texture : GraphicsResource</c>; one internal <see cref="CNA.Graphics.Texture3D"/>
/// owns and services the native resource.</summary>
public class Texture3D : Texture
{
    /// <summary>Wraps an already-created native handle -- the landing point for a texture read back
    /// out of a shader parameter. <c>protected internal</c> so <see cref="EffectParameter"/> can
    /// reach it, matching <see cref="Texture2D"/>'s own raw-handle constructor.</summary>
    internal Texture3D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.Texture3D(graphicsDevice.Framework, nativeHandleValue))
    {
    }

    internal Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : this(graphicsDevice, new CNA.Graphics.Texture3D(graphicsDevice.Framework, width, height, depth))
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, new CNA.Graphics.Texture3D(
            graphicsDevice.Framework, width, height, depth, mipMap, (CNA.Graphics.SurfaceFormat)(int)format))
    {
    }

    private Texture3D(GraphicsDevice graphicsDevice, CNA.Graphics.Texture3D frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
    }

    private CNA.Graphics.Texture3D FrameworkTexture3D => (CNA.Graphics.Texture3D)FrameworkTexture;

    public int Width => FrameworkTexture3D.Width;

    public int Height => FrameworkTexture3D.Height;

    public int Depth => FrameworkTexture3D.Depth;

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
        ArgumentNullException.ThrowIfNull(data);
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
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);
        RequireColorElement<T>();

        CNA.Color[] values = FrameworkTexture3D.GetData(
            level, left, top, right, bottom, front, back);
        if (values.Length > elementCount)
        {
            throw new ArgumentException("The destination window is too small for the requested volume.", nameof(elementCount));
        }

        for (int i = 0; i < values.Length; i++)
        {
            data[startIndex + i] = (T)(object)(Color)values[i];
        }
    }

    private static CNA.Color[] ConvertColors<T>(T[] source) where T : struct
    {
        RequireColorElement<T>();
        var result = new CNA.Color[source.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (Color)(object)source[i];
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
}
