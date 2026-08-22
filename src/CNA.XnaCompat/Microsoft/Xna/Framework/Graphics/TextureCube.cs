namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>TextureCube</c>. See <see cref="Texture3D"/>'s own doc comment
/// for the pattern.</summary>
public class TextureCube : Texture
{
    internal TextureCube(GraphicsDevice graphicsDevice, int size)
        : this(graphicsDevice, new CNA.Graphics.TextureCube(graphicsDevice.Framework, size))
    {
    }

    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, new CNA.Graphics.TextureCube(
            graphicsDevice.Framework, size, mipMap, (CNA.Graphics.SurfaceFormat)(int)format))
    {
    }

    /// <summary>Forwards an already-created handle, for <see cref="RenderTargetCube"/> -- see the
    /// base class's own equivalent constructor.</summary>
    internal TextureCube(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.TextureCube(graphicsDevice.Framework, nativeHandleValue))
    {
    }

    internal TextureCube(GraphicsDevice graphicsDevice, CNA.Graphics.TextureCube frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
    }

    private CNA.Graphics.TextureCube FrameworkTextureCube => (CNA.Graphics.TextureCube)FrameworkTexture;

    public int Size => FrameworkTextureCube.Size;

    protected override void Dispose(bool arg0)
    {
        if (!IsDisposed)
        {
            DisposeFrameworkTexture();
        }

        base.Dispose(arg0);
    }

    public void SetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(cubeMapFace, 0, null, data, 0, data.Length);
    }

    public void SetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(cubeMapFace, 0, null, data, startIndex, elementCount);
    }

    public void SetData<T>(
        CubeMapFace cubeMapFace,
        int level,
        Rectangle? rect,
        T[] data,
        int startIndex,
        int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        FrameworkTextureCube.SetData(
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace, level,
            rect.ToFramework(),
            ConvertColors(data), startIndex, elementCount);
    }

    public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(cubeMapFace, 0, null, data, 0, data.Length);
    }

    public void GetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(cubeMapFace, 0, null, data, startIndex, elementCount);
    }

    public void GetData<T>(
        CubeMapFace cubeMapFace,
        int level,
        Rectangle? rect,
        T[] data,
        int startIndex,
        int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);
        RequireColorElement<T>();

        CNA.Color[] values = FrameworkTextureCube.GetData(
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace, level,
            rect.ToFramework());
        if (values.Length > elementCount)
        {
            throw new ArgumentException("The destination window is too small for the requested face.", nameof(elementCount));
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
                $"CNA's current TextureCube C ABI transfers Color elements only; {typeof(T)} requires an upstream typed/raw route.");
        }
    }
}
