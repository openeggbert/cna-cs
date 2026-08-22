using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>RenderTargetCube</c>: a cube texture that can also be bound as the active
/// render target, one face at a time, via <see cref="GraphicsDevice.SetRenderTarget(RenderTargetCube?, CubeMapFace)"/>.
///
/// Subclasses <see cref="TextureCube"/>, matching real XNA's own
/// <c>RenderTargetCube : TextureCube</c>, and overrides the pieces that differ: creation goes
/// through <c>cna_render_target_cube_create</c> and release through the shared
/// <c>cna_render_target_destroy</c> -- render targets are the one family whose destroy route is
/// shared across shapes, unlike plain textures where every kind has its own. Its
/// <see cref="TextureCube.Size"/>/<see cref="TextureCube.LevelCount"/>/<see cref="TextureCube.Format"/> still read through
/// the inherited <c>cna_texturecube_get_info</c>, which the C API accepts for a cube render
/// target's handle the same way <c>cna_texture_get_info</c> does.
/// </summary>
public class RenderTargetCube : TextureCube
{
    public RenderTargetCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
        : this(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents)
    {
    }

    public RenderTargetCube(
        GraphicsDevice graphicsDevice,
        int size,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
        : base(graphicsDevice, CreateNativeCubeHandle(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage))
    {
    }

    /// <summary>Creates the native cube render target without wrapping it, so the result can be
    /// handed to a handle-wrapping constructor. <c>internal</c> (not private) so CNA.XnaCompat's
    /// own <c>RenderTargetCube</c> -- which derives from CNA.XnaCompat's <c>TextureCube</c> and so
    /// cannot inherit this type -- makes the identical native call rather than duplicating it.
    /// The same shape <c>RenderTarget2D.CreateNativeHandle</c> uses, and for the same reason (a
    /// <c>base(...)</c> argument is evaluated before any instance state exists, so the native call
    /// cannot live in the constructor body).</summary>
    internal static nint CreateNativeCubeHandle(
        GraphicsDevice graphicsDevice,
        int size,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        ArgumentOutOfRangeException.ThrowIfNegative(preferredMultiSampleCount);

        var createInfo = new CnaRenderTargetCubeCreateInfo
        {
            Size = (uint)size,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)preferredFormat,
            DepthFormat = (uint)preferredDepthFormat,
            MultiSampleCount = preferredMultiSampleCount,
            Usage = (uint)usage,
        };

        CnaResult result = Native.cna_render_target_cube_create(
            graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(RenderTargetCube));

        return handle.AsNint;
    }

    /// <summary>Matches <c>cna_render_target_destroy</c> -- not <c>cna_texturecube_destroy</c>,
    /// which this type would otherwise inherit and which does not own this handle.</summary>
    protected override void ReleaseNative(nint handleValue) => RenderTarget2D.ReleaseNativeRenderTarget(handleValue);

    /// <summary>Shares <see cref="RenderTarget2D"/>'s reader -- <c>cna_render_target_get_info</c>
    /// serves both kinds and reports which through its own <c>kind</c> field.</summary>
    private static CnaRenderTargetInfo GetInfo(nint handleValue) => RenderTarget2D.GetInfo(handleValue);

    /// <summary>The depth/stencil format this target was created with.</summary>
    public DepthFormat DepthStencilFormat => (DepthFormat)GetInfo(NativeHandleValue).DepthStencilFormat;

    /// <summary>How many samples this target multisamples with. Zero when it does not.</summary>
    public int MultiSampleCount => GetInfo(NativeHandleValue).MultiSampleCount;

    /// <summary>Whether contents survive a device reset or a rebind.</summary>
    public RenderTargetUsage RenderTargetUsage => (RenderTargetUsage)GetInfo(NativeHandleValue).Usage;

    /// <summary>Whether a device reset has discarded this target's contents. Read from native
    /// (<c>CNA_RenderTargetInfo.content_lost</c>) rather than hardcoded -- the same correction
    /// <see cref="DynamicVertexBuffer.IsContentLost"/> already got.</summary>
    public bool IsContentLost => GetInfo(NativeHandleValue).ContentLost != 0;

    /// <summary>
    /// Raised when a device reset discards this target's contents.
    ///
    /// <c>render_target.h</c> has no per-target subscription route -- unlike
    /// <c>vertex_resources.h</c>/<c>index_resources.h</c>, which do. So this is currently inert,
    /// and says so rather than pretending: <see cref="IsContentLost"/> is the real signal, and a
    /// game that needs to react polls it after a reset. Closing this needs a
    /// <c>cna_render_target_subscribe_content_lost</c> upstream.
    /// </summary>
    public event EventHandler<EventArgs>? ContentLost
    {
        add => _contentLost += value;
        remove => _contentLost -= value;
    }

    private EventHandler<EventArgs>? _contentLost;
}
