namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTarget2D</c>. Inherits from this namespace's own <c>Texture2D</c>
/// (not <c>CNA.Graphics.RenderTarget2D</c>) so <c>Texture2D t = someRenderTarget;</c> compiles in
/// game code, matching real XNA's <c>RenderTarget2D : Texture2D</c>. Creation still goes through
/// <c>CNA.Graphics.RenderTarget2D.CreateNativeHandle</c> -- see that method's doc comment for why
/// it exists and why this doesn't just subclass it directly.
///
/// Because this class's base is CNA.XnaCompat's own <c>Texture2D</c> rather than
/// <c>CNA.Graphics.RenderTarget2D</c>, it does not inherit that type's own
/// <c>ReleaseNative</c>/<c>Width</c>/<c>Height</c> overrides -- without overriding them here too,
/// this class would silently call the plain-texture natives on a handle that
/// <c>cna_render_target2d_create</c> actually created, exactly the bug this migration's step 5
/// fixed on the CNA.Framework side. Reuses <c>CNA.Graphics.RenderTarget2D</c>'s own
/// <c>internal static</c> helpers rather than duplicating the native calls.
/// </summary>
public class RenderTarget2D : Texture2D, IDynamicGraphicsResource
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, new CNA.Graphics.RenderTarget2D(graphicsDevice.Framework, width, height))
    {
    }

    public RenderTarget2D(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat)
        : this(
            graphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat,
            0, RenderTargetUsage.DiscardContents)
    {
    }

    public RenderTarget2D(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
        : base(graphicsDevice, new CNA.Graphics.RenderTarget2D(
            graphicsDevice.Framework,
            width,
            height,
            mipMap,
            (CNA.Graphics.SurfaceFormat)(int)preferredFormat,
            (CNA.Graphics.DepthFormat)(int)preferredDepthFormat,
            preferredMultiSampleCount,
            (CNA.Graphics.RenderTargetUsage)(int)usage))
    {
    }

    /// <summary>Reads from <c>cna_render_target_get_info</c> through
    /// <c>CNA.Graphics.RenderTarget2D</c>'s own internal reader, the same source its inherited
    /// texture dimensions use -- this class derives from its own
    /// namespace's texture base, not from <c>CNA.Graphics.RenderTarget2D</c>, so there is no base
    /// property to re-type.</summary>
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
