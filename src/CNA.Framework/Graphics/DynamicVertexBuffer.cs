using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>DynamicVertexBuffer</c>: a vertex buffer optimised for frequent rewriting
/// from the CPU.
///
/// A thin subclass rather than its own binding, because that is what the ABI models: the same
/// <c>cna_vertex_buffer_create</c> takes a <c>dynamic</c> flag in its create-info
/// (<c>vertex_resources.h</c>), so a dynamic buffer is the identical native resource with one bit
/// set -- not a separate type. Everything else (<c>SetData</c>, <c>GetData</c>, disposal) is
/// inherited unchanged and needs no override.
///
/// The two exceptions are <see cref="IsContentLost"/> and <see cref="ContentLost"/>, which were a
/// hardcoded <see langword="false"/> and an inert <c>add { } remove { }</c> pair, on the
/// recorded grounds that device-reset content loss is "a Direct3D 9-era concept the C API has no
/// counterpart for". It has both: <c>CNA_VertexBufferInfo.is_content_lost</c>
/// (<c>vertex_resources.h:67</c>) and <c>cna_vertex_buffer_subscribe_content_lost</c>
/// (<c>:365</c>). The header does say CNA never raises it today -- a fact about this renderer, not
/// about the ABI, and reading it means a renderer that starts reporting loss is reported here with
/// no change to this file.
/// </summary>
public class DynamicVertexBuffer : VertexBuffer
{
    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)
    {
    }

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, dynamic: true)
    {
    }

    private NativeEventBridge? _contentLostBridge;
    private EventHandler<EventArgs>? _contentLost;
    private bool _contentLostDisposed;
    private readonly object _contentLostLock = new();

    /// <summary>
    /// Whether a device reset has discarded this buffer's contents. Read from native, not a
    /// hardcoded <see langword="false"/>.
    ///
    /// The header says CNA currently never raises ContentLost -- a fact about this renderer today,
    /// not the absence of the concept, which is what the previous "the C API has no counterpart
    /// for it" note claimed while `CNA_VertexBufferInfo` had carried an `is_content_lost` field and
    /// `cna_vertex_buffer_subscribe_content_lost` had existed all along. Reading it means a renderer that
    /// starts reporting loss is reported here without a change to this file.
    /// </summary>
    public bool IsContentLost
        => QueryIsContentLost(NativeHandleValue, this);

    /// <summary>
    /// Raised when a device reset discards this buffer's contents. A real native subscription now,
    /// not an inert <c>add { } remove { }</c> pair.
    ///
    /// Taken on the first <c>+=</c> and held until <see cref="Dispose"/>, for the reason
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> records. The native callback carries the
    /// buffer handle alongside the context; it is ignored, because the handler is already bound to
    /// this object.
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

    /// <summary>Releases the subscription before the base releases the buffer handle it is
    /// registered against. Any handler failure captured but never surfaced is rethrown last.</summary>
    protected override void Dispose(bool disposing)
    {
        NativeEventBridge? bridge;
        lock (_contentLostLock)
        {
            _contentLostDisposed = true;
            bridge = _contentLostBridge;
            _contentLostBridge = null;
            _contentLost = null;
        }

        Exception? pending = null;
        if (bridge is not null)
        {
            try
            {
                bridge.ThrowPendingException();
            }
            catch (Exception ex)
            {
                pending = ex;
            }

            bridge.Dispose();
        }

        base.Dispose(disposing);

        if (pending is not null)
        {
            throw pending;
        }
    }

    internal static bool QueryIsContentLost(nint nativeHandleValue, object lifetimeOwner)
    {
        var info = new CnaVertexBufferInfo();
        CnaResult result = Native.cna_vertex_buffer_get_info(new CnaHandle(nativeHandleValue), ref info);
        GC.KeepAlive(lifetimeOwner);
        CnaException.ThrowIfFailed(result, nameof(IsContentLost));
        return info.IsContentLost != 0;
    }

    internal static NativeEventBridge SubscribeContentLost(
        nint nativeHandleValue,
        object lifetimeOwner,
        Action dispatch) =>
        NativeEventBridge.SubscribeWithSender(
            dispatch,
            (callback, context) =>
            {
                CnaResult result = Native.cna_vertex_buffer_subscribe_content_lost(
                    new CnaHandle(nativeHandleValue), callback, context, out CnaHandle registration);
                GC.KeepAlive(lifetimeOwner);
                CnaException.ThrowIfFailed(result, nameof(ContentLost));
                return registration;
            },
            registration => Native.cna_vertex_buffer_unsubscribe_content_lost(registration));
}
