namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible dynamic vertex buffer. It is publicly a
/// <see cref="VertexBuffer"/> while the inherited CNA resource is created with its native dynamic
/// flag set.</summary>
public class DynamicVertexBuffer : VertexBuffer, IDynamicGraphicsResource
{
    private CNA.NativeEventBridge? _contentLostBridge;
    private EventHandler<EventArgs>? _contentLost;
    private bool _disposed;
    private readonly object _contentLostLock = new();

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage usage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, usage)
    {
    }

    public DynamicVertexBuffer(
        GraphicsDevice graphicsDevice,
        VertexDeclaration vertexDeclaration,
        int vertexCount,
        BufferUsage usage)
        : base(graphicsDevice, vertexDeclaration, vertexCount, usage, dynamic: true)
    {
        DisposeHook = DisposeDynamicState;
    }

    public bool IsContentLost => CNA.Graphics.DynamicVertexBuffer.QueryIsContentLost(NativeHandleValue, this);

    public virtual event EventHandler<EventArgs>? ContentLost
    {
        add
        {
            lock (_contentLostLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _contentLostBridge ??= CNA.Graphics.DynamicVertexBuffer.SubscribeContentLost(
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
        SetData(0, data, startIndex, elementCount, VertexDeclaration.VertexStride, options);

    public void SetData<T>(
        int offsetInBytes,
        T[] data,
        int startIndex,
        int elementCount,
        int vertexStride,
        SetDataOptions options)
        where T : struct
    {
        _ = options;
        base.SetData(offsetInBytes, data, startIndex, elementCount, vertexStride);
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
