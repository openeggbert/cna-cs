using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>Texture3D</c>. A volume texture: <see cref="Width"/>/<see cref="Height"/>/
/// <see cref="Depth"/> plus everything <see cref="Texture"/> already provides.
///
/// Its own real resource type upstream (<c>texture_volume.h</c>: its own
/// <c>create</c>/<c>get_info</c>/<c>destroy</c> triple), not a 2D texture with extra flags -- the
/// same relationship <see cref="RenderTarget2D"/> has to <see cref="Texture2D"/>.
///
/// <c>SetData</c>/<c>GetData</c> are not implemented yet. The real ABI has them
/// (<c>cna_texture3d_set_data</c>/<c>_get_data</c>/<c>_set_data_bytes</c>), but they go through a
/// <c>CNA_Texture3DTransfer</c> descriptor (mip box + caller-array window) that the 2D path has no
/// equivalent of, so they want their own increment rather than a rushed partial binding here.
/// </summary>
public class Texture3D : Texture
{
    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : this(graphicsDevice, width, height, depth, mipMap: false, SurfaceFormat.Color)
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, CreateNativeHandle(graphicsDevice, width, height, depth, mipMap, format))
    {
    }

    private static nint CreateNativeHandle(
        GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);

        var createInfo = new CnaTexture3DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            Depth = (uint)depth,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)format,
        };

        CnaResult result = Native.cna_texture3d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(Texture3D));
        return handle.AsNint;
    }

    protected override void ReleaseNative(nint handleValue) => Native.cna_texture3d_destroy(new CnaHandle(handleValue));

    public int Width => (int)GetInfo().Width;

    public int Height => (int)GetInfo().Height;

    public int Depth => (int)GetInfo().Depth;

    /// <summary>Overridden to read <c>cna_texture3d_get_info</c> rather than the shared
    /// <c>cna_texture_get_info</c> the base uses. Both would answer correctly -- the shared one
    /// accepts a Texture3D handle -- but this way a single native call already being made for
    /// dimensions also serves these, instead of a second round trip.</summary>
    public override int LevelCount => (int)GetInfo().LevelCount;

    public override SurfaceFormat Format => (SurfaceFormat)GetInfo().Format;

    private CnaTexture3DInfo GetInfo()
    {
        var info = new CnaTexture3DInfo();
        CnaResult result = Native.cna_texture3d_get_info(new CnaHandle(NativeHandleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texture3d_get_info");
        return info;
    }
}
