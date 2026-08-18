namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>GraphicsResource</c> abstract base: the common
/// <see cref="GraphicsDevice"/>/<see cref="Name"/>/<see cref="Tag"/>/<see cref="IsDisposed"/>/
/// <see cref="Disposing"/> surface every GPU-backed resource shares.
///
/// Introduced by Phase 8 WP1 (see <c>plan.md</c>). Nothing derives from it yet -- reparenting the
/// existing resource types (<see cref="Texture2D"/>, <see cref="RenderTarget2D"/>,
/// <see cref="VertexBuffer"/>, <see cref="IndexBuffer"/>, <see cref="Effect"/>,
/// <see cref="SpriteFont"/>) onto it is WP3's job, deliberately kept separate: each of those types
/// currently owns its native handle through its own <c>NativeResourceHandle</c> field and a
/// non-virtual <c>Dispose()</c>, so reparenting is a real, behavior-touching refactor of every one
/// of them rather than a mechanical base-class swap, and it wants its own increment (and its own
/// review pass) instead of riding along with a batch of enum additions.
/// </summary>
public abstract class GraphicsResource : IDisposable
{
    private bool _disposed;

    protected GraphicsResource(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        GraphicsDevice = graphicsDevice;
    }

    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>Purely a managed-side label, exactly as in real XNA -- it is never sent to native
    /// and carries no engine meaning.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Arbitrary caller-owned payload, exactly as in real XNA. Never read by this
    /// project.</summary>
    public object? Tag { get; set; }

    public bool IsDisposed => _disposed;

    /// <summary>Raised once, immediately before this resource releases its native handle. Matches
    /// real XNA's own <c>Disposing</c> event, which fires on the way *into* disposal rather than
    /// after it, so a handler can still read the resource's own state.</summary>
    public event EventHandler<EventArgs>? Disposing;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Idempotent: a second call is a documented no-op, matching real XNA and this
    /// project's existing <c>NativeResourceHandle</c>-based resources. An override must call
    /// <c>base.Dispose(disposing)</c> so <see cref="Disposing"/> still fires and
    /// <see cref="IsDisposed"/> still flips.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }

        _disposed = true;
    }

    /// <summary>Matches real XNA, which returns <see cref="Name"/> when one was set and falls back
    /// to the default <see cref="object.ToString"/> type name otherwise.</summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? base.ToString()! : Name;
}
