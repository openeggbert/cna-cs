using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DynamicIndexBuffer</c>. See
/// <see cref="DynamicVertexBuffer"/>'s own doc comment -- identical rationale throughout, including
/// for <see cref="IsContentLost"/>/<see cref="ContentLost"/>, whose routes are
/// <c>index_resources.h:66</c> and <c>:197</c>.</summary>
public class DynamicIndexBuffer : IndexBuffer
{
    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, SizeForType(indexType), indexCount, bufferUsage)
    {
    }

    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, indexElementSize, indexCount, bufferUsage, dynamic: true)
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
    /// for it" note claimed while `CNA_IndexBufferInfo` had carried an `is_content_lost` field and
    /// `cna_index_buffer_subscribe_content_lost` had existed all along. Reading it means a renderer that
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
        var info = new CnaIndexBufferInfo();
        CnaResult result = Native.cna_index_buffer_get_info(new CnaHandle(nativeHandleValue), ref info);
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
                CnaResult result = Native.cna_index_buffer_subscribe_content_lost(
                    new CnaHandle(nativeHandleValue), callback, context, out CnaHandle registration);
                GC.KeepAlive(lifetimeOwner);
                CnaException.ThrowIfFailed(result, nameof(ContentLost));
                return registration;
            },
            registration => Native.cna_index_buffer_unsubscribe_content_lost(registration));
}
