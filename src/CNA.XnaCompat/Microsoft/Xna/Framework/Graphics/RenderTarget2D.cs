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
/// <c>cna_render_target2d_create</c> actually created. It reuses
/// <c>CNA.Graphics.RenderTarget2D</c>'s
/// <c>internal static</c> helpers rather than duplicating the native calls.
/// </summary>
public class RenderTarget2D : Texture2D, IDynamicGraphicsResource
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, CreateFrameworkRenderTarget(
            graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None,
            0, RenderTargetUsage.DiscardContents))
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
        : base(graphicsDevice, CreateFrameworkRenderTarget(
            graphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat,
            preferredMultiSampleCount, usage))
    {
    }

    /// <summary>Reads from <c>cna_render_target_get_info</c> through
    /// <c>CNA.Graphics.RenderTarget2D</c>'s own internal reader, the same source used to cache the
    /// texture dimensions -- this class derives from its own
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

    /// <summary>
    /// A real native subscription since CNA 0.19.0 added
    /// <c>cna_render_target_subscribe_content_lost</c>; it replaces an inert
    /// <c>add</c>/<c>remove</c> pair. Routed through <c>CNA.Graphics.RenderTarget2D</c>'s
    /// <c>internal static</c> helper for the same reason <see cref="DepthStencilFormat"/> is:
    /// this class derives from its own namespace's texture base, so there is no inherited
    /// implementation to reuse and it must not name a <c>CNA.Interop</c> type.
    ///
    /// Only a renderer family that can genuinely lose a device reports one; on the rest the
    /// subscription is valid and silent, and a caller-initiated <c>Reset</c> is not loss.
    /// </summary>
    public virtual event EventHandler<EventArgs>? ContentLost
    {
        add
        {
            lock (_contentLostLock)
            {
                ObjectDisposedException.ThrowIf(_contentLostDisposed, this);

                _contentLostBridge ??= CNA.Graphics.RenderTarget2D.SubscribeContentLost(
                    NativeHandleValue, this, () => _contentLost?.Invoke(this, EventArgs.Empty));

                _contentLost += value;
            }
        }
        remove
        {
            lock (_contentLostLock)
            {
                _contentLost -= value;
            }
        }
    }

    private CNA.NativeEventBridge? _contentLostBridge;
    private EventHandler<EventArgs>? _contentLost;
    private bool _contentLostDisposed;
    private readonly object _contentLostLock = new();

    /// <summary>Releases the subscription before the base releases the render-target handle it is
    /// registered against.</summary>
    protected override void Dispose(bool arg0)
    {
        CNA.NativeEventBridge? bridge;
        lock (_contentLostLock)
        {
            _contentLostDisposed = true;
            bridge = _contentLostBridge;
            _contentLostBridge = null;
            _contentLost = null;
        }

        Exception? pending = CNA.Graphics.RenderTarget2D.DrainContentLostBridge(bridge);
        base.Dispose(arg0);

        if (pending is not null)
        {
            throw pending;
        }
    }

    private static CNA.Graphics.RenderTarget2D CreateFrameworkRenderTarget(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        Texture2D.ValidateDimensions(width, height);

        // XNA's adapter query maps every request of one sample or less to no multisampling.
        int selectedMultiSampleCount = preferredMultiSampleCount <= 1 ? 0 : preferredMultiSampleCount;
        return new CNA.Graphics.RenderTarget2D(
            graphicsDevice.Framework,
            width,
            height,
            mipMap,
            (CNA.Graphics.SurfaceFormat)(int)preferredFormat,
            (CNA.Graphics.DepthFormat)(int)preferredDepthFormat,
            selectedMultiSampleCount,
            (CNA.Graphics.RenderTargetUsage)(int)usage);
    }
}
