using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>TextureCube</c>: six square faces of equal <see cref="Size"/>, addressed
/// by <see cref="CubeMapFace"/>. Its own real resource type upstream
/// (<c>texture_volume.h</c>) -- see <see cref="Texture3D"/>'s doc comment, which describes the
/// same relationship and the same reason <c>SetData</c>/<c>GetData</c> are deferred to their own
/// increment (here the descriptor is <c>CNA_TextureCubeTransfer</c>, which adds a face selector on
/// top of the mip rectangle).
/// </summary>
public class TextureCube : Texture
{
    public TextureCube(GraphicsDevice graphicsDevice, int size)
        : this(graphicsDevice, size, mipMap: false, SurfaceFormat.Color)
    {
    }

    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, CreateNativeHandle(graphicsDevice, size, mipMap, format))
    {
    }

    /// <summary>Wraps an already-created native handle. <c>protected</c> so
    /// <see cref="RenderTargetCube"/> -- which creates its handle through an entirely different
    /// native route (<c>cna_render_target_cube_create</c>) -- can still reuse this type's
    /// accessors, matching real XNA's <c>RenderTargetCube : TextureCube</c>.</summary>
    protected TextureCube(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    private static nint CreateNativeHandle(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var createInfo = new CnaTextureCubeCreateInfo
        {
            Size = (uint)size,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)format,
        };

        CnaResult result = Native.cna_texturecube_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(TextureCube));
        return handle.AsNint;
    }

    protected override void ReleaseNative(nint handleValue) => Native.cna_texturecube_destroy(new CnaHandle(handleValue));

    /// <summary>Width and height of each square face. Real XNA spells this <c>Size</c> on
    /// <c>TextureCube</c> (not <c>Width</c>/<c>Height</c>), matching the C API's own field
    /// name.</summary>
    public int Size => (int)GetInfo().Size;

    public override int LevelCount => (int)GetInfo().LevelCount;

    public override SurfaceFormat Format => (SurfaceFormat)GetInfo().Format;

    private CnaTextureCubeInfo GetInfo()
    {
        var info = new CnaTextureCubeInfo();
        CnaResult result = Native.cna_texturecube_get_info(new CnaHandle(NativeHandleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texturecube_get_info");
        return info;
    }
}
