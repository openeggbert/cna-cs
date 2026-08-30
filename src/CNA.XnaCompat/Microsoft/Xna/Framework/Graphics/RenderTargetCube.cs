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
        : base(graphicsDevice, CreateFrameworkRenderTarget(
            graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat,
            preferredMultiSampleCount, usage))
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

    private static CNA.Graphics.RenderTargetCube CreateFrameworkRenderTarget(
        GraphicsDevice graphicsDevice,
        int size,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        TextureCube.ValidateSize(size);
        int selectedMultiSampleCount = preferredMultiSampleCount <= 1 ? 0 : preferredMultiSampleCount;
        return new CNA.Graphics.RenderTargetCube(
            graphicsDevice.Framework,
            size,
            mipMap,
            (CNA.Graphics.SurfaceFormat)(int)preferredFormat,
            (CNA.Graphics.DepthFormat)(int)preferredDepthFormat,
            selectedMultiSampleCount,
            (CNA.Graphics.RenderTargetUsage)(int)usage);
    }
}
