using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A texture that can be set as the active render target via
/// <see cref="GraphicsDevice.SetRenderTarget(Texture2D?)"/>. Subclasses <see cref="Texture2D"/> (matching
/// real XNA's <c>RenderTarget2D : Texture2D</c>), but -- now that the real, shipped
/// openeggbert/cna C API confirms <c>RenderTarget2D</c> is its own real resource type upstream
/// (<c>render_target.h</c>: its own <c>create</c>/<c>get_info</c>/<c>destroy</c> routes, none of
/// them <c>cna_texture2d_*</c>) -- overrides <see cref="Texture2D.ReleaseNative"/>/
/// <see cref="Texture2D.Width"/>/<see cref="Texture2D.Height"/> to call those real functions
/// instead of the plain-texture ones it inherits. See <c>NEXT.md</c>'s native-ABI-migration entry,
/// step 5, for why this was previously self-designed against a texture-shaped handle.
///
/// <see cref="ReleaseNativeRenderTarget"/>/<see cref="GetDimensions"/> are <c>internal static</c>
/// (not just the override bodies) specifically so CNA.XnaCompat's own
/// <c>RenderTarget2D</c> -- which inherits from CNA.XnaCompat's own <c>Texture2D</c>, not this
/// type, so it cannot inherit these overrides -- can call the same real native functions without
/// ever naming a CNA.Interop type itself (<see cref="GetDimensions"/> returns a plain tuple, not
/// <see cref="CnaRenderTargetInfo"/>, for exactly that reason -- see docs/architecture.md).
/// </summary>
public class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(
            graphicsDevice,
            width,
            height,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents)
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
        : base(graphicsDevice, CreateNativeHandle(
            graphicsDevice,
            width,
            height,
            mipMap,
            preferredFormat,
            preferredDepthFormat,
            preferredMultiSampleCount,
            usage))
    {
    }

    /// <summary>
    /// Creates the native render-target handle without wrapping it. <c>internal</c> (visible to
    /// CNA.XnaCompat via the assembly's <c>InternalsVisibleTo</c> grant) so CNA.XnaCompat's
    /// <c>RenderTarget2D</c> can reuse this same native call while still inheriting from
    /// CNA.XnaCompat's own <c>Texture2D</c> -- matching real XNA's <c>RenderTarget2D : Texture2D</c>
    /// ancestry so <c>Texture2D t = someRenderTarget;</c> compiles in game code -- instead of from
    /// this type, which the usual "derive from the CNA.Framework type, forward a protected internal
    /// ctor" trick would require.
    /// </summary>
    internal static nint CreateNativeHandle(GraphicsDevice graphicsDevice, int width, int height)
        => CreateNativeHandle(
            graphicsDevice,
            width,
            height,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);

    internal static nint CreateNativeHandle(
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

        var createInfo = new CnaRenderTarget2DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            MipMap = (byte)(mipMap ? 1 : 0),
            ColorFormat = (uint)preferredFormat,
            DepthStencilFormat = (uint)preferredDepthFormat,
            MultiSampleCount = preferredMultiSampleCount,
            Usage = (uint)usage,
        };

        CnaResult result = Native.cna_render_target2d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(RenderTarget2D));

        return handle.AsNint;
    }

    /// <summary>Matches <c>cna_render_target_destroy</c> exactly (<c>render_target.h:277</c>).
    /// <c>internal static</c>, not just this override's body -- see this class's own doc comment
    /// for why CNA.XnaCompat's parallel <c>RenderTarget2D</c> needs to call it directly too.</summary>
    internal static bool ReleaseNativeRenderTarget(nint handleValue) =>
        Native.cna_render_target_destroy(new CnaHandle(handleValue)).IsSuccess();

    protected override bool ReleaseNative(nint handleValue) => ReleaseNativeRenderTarget(handleValue);

    /// <summary>Matches <c>cna_render_target_get_info</c> exactly (<c>render_target.h:199</c>) --
    /// <c>texture.h</c>'s/<c>graphics.h</c>'s own texture-info routes don't apply to a render
    /// target's real handle type. Returns a plain tuple (not <see cref="CnaRenderTargetInfo"/>) --
    /// see this class's own doc comment.</summary>
    internal static (int Width, int Height) GetDimensions(nint handleValue)
    {
        var info = new CnaRenderTargetInfo();
        CnaResult result = Native.cna_render_target_get_info(new CnaHandle(handleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_render_target_get_info");
        return ((int)info.Width, (int)info.Height);
    }

    public override int Width
    {
        get
        {
            int value = GetDimensions(NativeHandleValue).Width;
            GC.KeepAlive(this);
            return value;
        }
    }

    public override int Height
    {
        get
        {
            int value = GetDimensions(NativeHandleValue).Height;
            GC.KeepAlive(this);
            return value;
        }
    }

    /// <summary>Reads the whole info block. Kept alongside <see cref="GetDimensions"/> rather than
    /// replacing it: most callers want only the two dimensions, and this one exists because
    /// XNA exposes five more properties off the same native call.</summary>
    internal static CnaRenderTargetInfo GetInfo(nint handleValue)
    {
        var info = new CnaRenderTargetInfo();
        CnaResult result = Native.cna_render_target_get_info(new CnaHandle(handleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_render_target_get_info");
        return info;
    }

    /// <summary>
    /// The four XNA properties read off one info block, returned as public types rather than as
    /// <c>CnaRenderTargetInfo</c> -- the same reason <see cref="GetDimensions"/> returns a tuple:
    /// CNA.XnaCompat's parallel render targets need to call this, and they cannot name a
    /// <c>CNA.Interop</c> type.
    /// </summary>
    internal static (DepthFormat DepthStencilFormat, RenderTargetUsage Usage, int MultiSampleCount, bool ContentLost)
        GetRenderTargetProperties(nint handleValue)
    {
        CnaRenderTargetInfo info = GetInfo(handleValue);
        return ((DepthFormat)info.DepthStencilFormat, (RenderTargetUsage)info.Usage,
                info.MultiSampleCount, info.ContentLost != 0);
    }

    /// <summary>The depth/stencil format this target was created with.</summary>
    public DepthFormat DepthStencilFormat
    {
        get
        {
            DepthFormat value = (DepthFormat)GetInfo(NativeHandleValue).DepthStencilFormat;
            GC.KeepAlive(this);
            return value;
        }
    }

    /// <summary>How many samples this target multisamples with. Zero when it does not.</summary>
    public int MultiSampleCount
    {
        get
        {
            int value = GetInfo(NativeHandleValue).MultiSampleCount;
            GC.KeepAlive(this);
            return value;
        }
    }

    /// <summary>Whether contents survive a device reset or a rebind.</summary>
    public RenderTargetUsage RenderTargetUsage
    {
        get
        {
            RenderTargetUsage value = (RenderTargetUsage)GetInfo(NativeHandleValue).Usage;
            GC.KeepAlive(this);
            return value;
        }
    }

    /// <summary>Whether a device reset has discarded this target's contents. Read from native
    /// (<c>CNA_RenderTargetInfo.content_lost</c>) rather than hardcoded -- the same correction
    /// <see cref="DynamicVertexBuffer.IsContentLost"/> already got.</summary>
    public bool IsContentLost
    {
        get
        {
            bool value = GetInfo(NativeHandleValue).ContentLost != 0;
            GC.KeepAlive(this);
            return value;
        }
    }

    /// <summary>
    /// Raised when a renderer loses and recreates its device, discarding this target's contents.
    ///
    /// A real native subscription since CNA 0.19.0, which added the
    /// <c>cna_render_target_subscribe_content_lost</c> this event's previous doc comment asked for.
    /// It replaces an inert <c>add</c>/<c>remove</c> pair that stored handlers and could never call
    /// them.
    ///
    /// <c>render_target.h</c> is explicit that only a renderer family which can genuinely lose a
    /// device reports one -- it names <c>DIRECTX9</c>, <c>DIRECT2D</c> and <c>SKIA</c>, and
    /// upstream is removing <c>SKIA</c> -- and that a caller-initiated <c>Reset</c> is not loss. On
    /// every other renderer the subscription is valid and silent, which is the renderer's answer
    /// rather than this binding's.
    ///
    /// Taken on the first <c>+=</c> and held until <see cref="Dispose"/>, for the reason
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> records.
    /// </summary>
    public event EventHandler<EventArgs>? ContentLost
    {
        add
        {
            lock (_contentLostLock)
            {
                ObjectDisposedException.ThrowIf(_contentLostDisposed, this);

                _contentLostBridge ??= SubscribeContentLost(
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

    private NativeEventBridge? _contentLostBridge;
    private EventHandler<EventArgs>? _contentLost;
    private bool _contentLostDisposed;
    private readonly object _contentLostLock = new();

    /// <summary>Releases the subscription before the base releases the render-target handle it is
    /// registered against -- the other order would leave native able to call back into a context
    /// whose owner is gone. Any handler failure captured but never surfaced is rethrown last.</summary>
    protected override void Dispose(bool disposing)
    {
        Exception? pending = ReleaseContentLostSubscription();
        base.Dispose(disposing);

        if (pending is not null)
        {
            throw pending;
        }
    }

    private Exception? ReleaseContentLostSubscription()
    {
        NativeEventBridge? bridge;
        lock (_contentLostLock)
        {
            _contentLostDisposed = true;
            bridge = _contentLostBridge;
            _contentLostBridge = null;
            _contentLost = null;
        }

        return DrainContentLostBridge(bridge);
    }

    /// <summary>
    /// The subscribe/unsubscribe pair, exposed as <c>internal static</c> over a plain
    /// <see langword="nint"/> for the reason <see cref="GetRenderTargetProperties"/> records:
    /// CNA.XnaCompat's parallel render targets need the same route and cannot name a
    /// <c>CNA.Interop</c> type. The native callback is <c>void(handle, void* context)</c>, so it
    /// goes through <see cref="NativeEventBridge.SubscribeWithSender"/>; the handle it carries is
    /// ignored because the handler is already bound to one object.
    ///
    /// One route serves both 2D and cube targets: <c>render_target.h</c> takes "an owned 2D or cube
    /// render-target handle".
    /// </summary>
    internal static NativeEventBridge SubscribeContentLost(
        nint nativeHandleValue,
        object lifetimeOwner,
        Action dispatch) =>
        NativeEventBridge.SubscribeWithSender(
            dispatch,
            (callback, context) =>
            {
                CnaResult result = Native.cna_render_target_subscribe_content_lost(
                    new CnaHandle(nativeHandleValue), callback, context, out CnaHandle registration);
                GC.KeepAlive(lifetimeOwner);
                CnaException.ThrowIfFailed(result, nameof(ContentLost));
                return registration;
            },
            registration => Native.cna_render_target_unsubscribe_content_lost(registration));

    /// <summary>Unsubscribes and returns a handler failure the bridge captured but never surfaced,
    /// so the caller can rethrow it after its own teardown rather than in the middle of it.</summary>
    internal static Exception? DrainContentLostBridge(NativeEventBridge? bridge)
    {
        if (bridge is null)
        {
            return null;
        }

        Exception? pending = null;
        try
        {
            bridge.ThrowPendingException();
        }
        catch (Exception ex)
        {
            pending = ex;
        }

        bridge.Dispose();
        return pending;
    }
}
