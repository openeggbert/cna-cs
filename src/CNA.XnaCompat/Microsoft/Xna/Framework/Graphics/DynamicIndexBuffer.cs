namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible dynamic index buffer with the required
/// <c>DynamicIndexBuffer : IndexBuffer</c> relationship.</summary>
public class DynamicIndexBuffer : IndexBuffer, IDynamicGraphicsResource
{
    private CNA.NativeEventBridge? _contentLostBridge;
    private EventHandler<EventArgs>? _contentLost;
    private bool _disposed;
    private readonly object _contentLostLock = new();

    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage usage)
        : this(
            graphicsDevice,
            (IndexElementSize)(int)CNA.Graphics.IndexBuffer.SizeForType(indexType),
            indexCount,
            usage)
    {
    }

    public DynamicIndexBuffer(
        GraphicsDevice graphicsDevice,
        IndexElementSize indexElementSize,
        int indexCount,
        BufferUsage usage)
        : base(graphicsDevice, indexElementSize, indexCount, usage, dynamic: true)
    {
        DisposeHook = DisposeDynamicState;
    }

    public bool IsContentLost => CNA.Graphics.DynamicIndexBuffer.QueryIsContentLost(NativeHandleValue, this);

    public virtual event EventHandler<EventArgs>? ContentLost
    {
        add
        {
            lock (_contentLostLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _contentLostBridge ??= CNA.Graphics.DynamicIndexBuffer.SubscribeContentLost(
                    NativeHandleValue,
                    this,
                    () => _contentLost?.Invoke(this, EventArgs.Empty));
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

    public void SetData<T>(T[] data, int startIndex, int elementCount, SetDataOptions options)
        where T : struct =>
        SetData(0, data, startIndex, elementCount, options);

    public void SetData<T>(
        int offsetInBytes,
        T[] data,
        int startIndex,
        int elementCount,
        SetDataOptions options)
        where T : struct
    {
        _ = options;
        base.SetData(offsetInBytes, data, startIndex, elementCount);
    }

    private Exception? DisposeDynamicState()
    {
        CNA.NativeEventBridge? bridge;
        lock (_contentLostLock)
        {
            _disposed = true;
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
            catch (Exception exception)
            {
                pending = exception;
            }

            bridge.Dispose();
        }

        return pending;
    }
}
