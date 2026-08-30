namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>TextureCube</c>. See <see cref="Texture3D"/>'s own doc comment
/// for the pattern.</summary>
public class TextureCube : Texture
{
    private readonly int _size;

    internal TextureCube(GraphicsDevice graphicsDevice, int size)
        : this(graphicsDevice, new CNA.Graphics.TextureCube(
            (graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice))).Framework,
            size))
    {
    }

    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : this(graphicsDevice, CreateFrameworkTexture(graphicsDevice, size, mipMap, format))
    {
    }

    /// <summary>Forwards an already-created handle, for <see cref="RenderTargetCube"/> -- see the
    /// base class's own equivalent constructor.</summary>
    internal TextureCube(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, new CNA.Graphics.TextureCube(
            (graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice))).Framework,
            nativeHandleValue))
    {
    }

    internal TextureCube(GraphicsDevice graphicsDevice, CNA.Graphics.TextureCube frameworkTexture)
        : base(graphicsDevice, frameworkTexture)
    {
        _size = frameworkTexture.Size;
    }

    private CNA.Graphics.TextureCube FrameworkTextureCube => (CNA.Graphics.TextureCube)FrameworkTexture;

    public int Size => _size;

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
        ValidateTransfer(cubeMapFace, level, rect, data, startIndex, elementCount);
        CNA.Graphics.TextureCube.SetFaceDataFrom(
            NativeHandleValue,
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace,
            level,
            rect.ToFramework(),
            data,
            startIndex,
            elementCount);
        GC.KeepAlive(this);
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
        ValidateTransfer(cubeMapFace, level, rect, data, startIndex, elementCount);
        CNA.Graphics.TextureCube.GetFaceDataInto(
            NativeHandleValue,
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace,
            level,
            rect.ToFramework(),
            data,
            startIndex,
            elementCount);
        GC.KeepAlive(this);
    }

    internal static void ValidateSize(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
    }

    private static CNA.Graphics.TextureCube CreateFrameworkTexture(
        GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ValidateSize(size);
        if (!Enum.IsDefined(format))
        {
            throw new NotSupportedException($"The surface format value {(int)format} is not supported.");
        }

        return new CNA.Graphics.TextureCube(
            graphicsDevice.Framework, size, mipMap, (CNA.Graphics.SurfaceFormat)(int)format);
    }

    private void ValidateTransfer<T>(
        CubeMapFace cubeMapFace,
        int level,
        Rectangle? rect,
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

        int mipSize = Math.Max(1, Size >> level);
        int transferWidth = mipSize;
        int transferHeight = mipSize;
        if (rect is { } region)
        {
            if (region.X < 0 || region.Y < 0 || region.Width <= 0 || region.Height <= 0 ||
                (long)region.X + region.Width > mipSize ||
                (long)region.Y + region.Height > mipSize)
            {
                throw new ArgumentException("The rectangle is outside the texture level.", nameof(rect));
            }

            transferWidth = region.Width;
            transferHeight = region.Height;
        }

        Texture2D.ValidateTransferSize<T>(Format, transferWidth, transferHeight, 1, elementCount);
        if (!Enum.IsDefined(cubeMapFace))
        {
            throw new InvalidOperationException("The cube-map face is invalid.");
        }
    }
}
