namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible base for resources associated with a graphics device.</summary>
public abstract class GraphicsResource : IDisposable
{
    private bool _disposed;
    private string _name = string.Empty;
    private GraphicsDevice? _graphicsDevice;

    internal GraphicsResource()
    {
    }

    internal GraphicsResource(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _graphicsDevice = graphicsDevice;
    }

    internal void AttachGraphicsDevice(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _graphicsDevice = graphicsDevice;
    }

    ~GraphicsResource()
    {
        Dispose(false);
    }

    public GraphicsDevice GraphicsDevice => _graphicsDevice!;

    public bool IsDisposed => _disposed;

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public object? Tag { get; set; }

    public event EventHandler<EventArgs>? Disposing;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool arg0)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (arg0)
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }

    public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString()! : Name;
}
