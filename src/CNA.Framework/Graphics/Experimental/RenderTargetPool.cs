using CNA.Interop;

namespace CNA.Graphics.Experimental;

/// <summary>
/// A render target borrowed from a <see cref="RenderTargetPool"/>.
///
/// <b>Borrowed, not owned.</b> The pool owns the underlying target; this is a view of it. Disposing
/// releases the view and leaves the target in the pool to be handed out again -- which is why the
/// type exists at all rather than returning a bare <see cref="RenderTarget2D"/> a caller would
/// reasonably assume they owned.
///
/// The pool refuses to reset or be destroyed while any view is outstanding, so failing to dispose
/// one is not a leak that goes unnoticed: it is a pool that will not reset.
/// </summary>
public sealed class PooledRenderTarget : IDisposable
{
    private readonly NativeResourceHandle _view;

    internal PooledRenderTarget(GraphicsDevice graphicsDevice, nint viewHandleValue)
    {
        // The wrapper is non-owning: this object releases the *view*, and the pool owns the target.
        // Two owners of one handle is the failure this separation exists to prevent.
        Target = RenderTarget2D.CreateBorrowed(graphicsDevice, viewHandleValue);
        _view = new NativeResourceHandle(
            viewHandleValue,
            h => Native.cna_render_target_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>The target itself, usable anywhere a <see cref="RenderTarget2D"/> is.</summary>
    public RenderTarget2D Target { get; }

    /// <summary>Releases the borrow. The pool keeps the target.</summary>
    public void Dispose() => _view.Dispose();
}

/// <summary>
/// A pool of reusable render targets, from CNA's engine layer.
///
/// <b>Experimental, and not XNA.</b> XNA has no such concept: a game allocates its own render
/// targets and manages them. CNA's post-process chain is built on this pool, and a game doing
/// multi-pass rendering by hand wants the same thing -- one target per shape and slot, reused across
/// frames instead of reallocated.
///
/// <b>Availability is asked, not assumed.</b> The routes resolve in every build; a build without the
/// engine layer answers <c>NOT_SUPPORTED</c> at call time. Symbol resolution therefore proves
/// nothing, and <see cref="GraphicsDevice.IsCnaEngineLayerAvailable"/> is the question to ask.
/// </summary>
public sealed class RenderTargetPool : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>Creates a pool against a device. Throws when this build has no engine layer, rather
    /// than returning a pool whose every operation would fail.</summary>
    public RenderTargetPool(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _graphicsDevice = graphicsDevice;
        CnaResult result = Native.cna_render_target_pool_create(
            graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle pool);
        CnaException.ThrowIfFailed(result, nameof(RenderTargetPool));

        _handle = new NativeResourceHandle(
            pool.AsNint,
            h => Native.cna_render_target_pool_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>
    /// Wraps a pool handle whose real owner is something else -- <see cref="PostProcessChain"/>'s
    /// own pool, handed out as a counted borrow.
    ///
    /// The release call is identical to an owned pool's, because that is how CNA models it: the
    /// same <c>pool_destroy</c> route destroys an owned pool and decrements a borrow. Only the
    /// meaning differs, so this is a separate factory with its own doc rather than a flag, and a
    /// caller cannot reach it without going through the chain that owns the pool.
    /// </summary>
    internal static RenderTargetPool CreateBorrowed(GraphicsDevice graphicsDevice, nint poolHandleValue) =>
        new(graphicsDevice, poolHandleValue);

    private RenderTargetPool(GraphicsDevice graphicsDevice, nint poolHandleValue)
    {
        _graphicsDevice = graphicsDevice;
        _handle = new NativeResourceHandle(
            poolHandleValue,
            h => Native.cna_render_target_pool_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>
    /// Borrows a target of this shape.
    ///
    /// <paramref name="slot"/> distinguishes two targets of identical shape that a pass needs at the
    /// same time -- a blur wanting a source and a destination of the same size asks for slot 0 and
    /// slot 1, and gets two targets rather than the same one twice.
    /// </summary>
    public PooledRenderTarget Acquire(
        int width, int height, SurfaceFormat format, DepthFormat depthFormat, int slot = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        CnaResult result = Native.cna_render_target_pool_acquire(
            Handle, width, height, (uint)format, (uint)depthFormat, slot, out CnaHandle target);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Acquire));

        return new PooledRenderTarget(_graphicsDevice, target.AsNint);
    }

    /// <summary>How many targets the pool is holding.</summary>
    public long TargetCount => Read(Native.cna_render_target_pool_get_target_count, nameof(TargetCount));

    /// <summary>What those targets are estimated to cost in bytes.</summary>
    public long EstimatedBytes => Read(Native.cna_render_target_pool_get_estimated_bytes, nameof(EstimatedBytes));

    /// <summary>
    /// Releases every pooled target.
    ///
    /// Refused by native while any <see cref="PooledRenderTarget"/> is still borrowed, which is the
    /// contract that makes the borrow safe rather than merely documented.
    /// </summary>
    public void Reset()
    {
        CnaResult result = Native.cna_render_target_pool_reset(Handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Reset));
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    private delegate CnaResult CountAccessor(CnaHandle pool, out ulong value);

    private long Read(CountAccessor accessor, string operation)
    {
        CnaResult result = accessor(Handle, out ulong value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, operation);
        return checked((long)value);
    }
}
