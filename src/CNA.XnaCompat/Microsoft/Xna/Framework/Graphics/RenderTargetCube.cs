namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTargetCube</c>. Derives from *this namespace's*
/// <see cref="TextureCube"/> (not <see cref="CNA.Graphics.RenderTargetCube"/>), so real XNA's own
/// <c>RenderTargetCube : TextureCube</c> ancestry holds here and
/// <c>TextureCube t = someRenderTargetCube;</c> compiles in game code -- the identical fork
/// <see cref="RenderTarget2D"/> already documents, resolved the same way: by reusing the base
/// assembly's <c>internal static</c> native helpers rather than duplicating any logic.
/// </summary>
public class RenderTargetCube : TextureCube, IDynamicGraphicsResource
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
        : base(graphicsDevice, new CNA.Graphics.RenderTargetCube(
            graphicsDevice.Framework,
            size,
            mipMap,
            (CNA.Graphics.SurfaceFormat)(int)preferredFormat,
            (CNA.Graphics.DepthFormat)(int)preferredDepthFormat,
            preferredMultiSampleCount,
            (CNA.Graphics.RenderTargetUsage)(int)usage))
    {
    }

    /// <summary>Reads from <c>cna_render_target_get_info</c> through
    /// <c>CNA.Graphics.RenderTarget2D</c>'s own internal reader, the same way
    /// this type's own dimensions already are -- it derives from its own
    /// namespace's texture base, not from <c>CNA.Graphics.RenderTarget2D</c>, so there is no base
    /// property to re-type. <c>cna_render_target_get_info</c> serves both kinds.</summary>
    public DepthFormat DepthStencilFormat =>
        (DepthFormat)(int)CNA.Graphics.RenderTarget2D.GetRenderTargetProperties(NativeHandleValue).DepthStencilFormat;

    /// <summary>See <see cref="DepthStencilFormat"/>.</summary>
    public RenderTargetUsage RenderTargetUsage =>
        (RenderTargetUsage)(int)CNA.Graphics.RenderTarget2D.GetRenderTargetProperties(NativeHandleValue).Usage;

    /// <summary>See <see cref="DepthStencilFormat"/>.</summary>
    public int MultiSampleCount => CNA.Graphics.RenderTarget2D.GetRenderTargetProperties(NativeHandleValue).MultiSampleCount;

    /// <summary>See <see cref="DepthStencilFormat"/>.</summary>
    public bool IsContentLost => CNA.Graphics.RenderTarget2D.GetRenderTargetProperties(NativeHandleValue).ContentLost;

    /// <summary>Inert -- <c>render_target.h</c> has no per-target subscription route. See
    /// <see cref="CNA.Graphics.RenderTarget2D.ContentLost"/>.</summary>
    public virtual event EventHandler<EventArgs>? ContentLost
    {
        add => _contentLost += value;
        remove => _contentLost -= value;
    }

    private EventHandler<EventArgs>? _contentLost;

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
