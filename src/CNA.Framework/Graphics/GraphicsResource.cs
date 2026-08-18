namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>GraphicsResource</c> abstract base: the common
/// <see cref="GraphicsDevice"/>/<see cref="Name"/>/<see cref="Tag"/>/<see cref="IsDisposed"/>/
/// <see cref="Disposing"/> surface every GPU-backed resource shares.
///
/// Introduced by Phase 8 WP1; <see cref="Texture"/> (and through it <see cref="Texture2D"/>,
/// <see cref="Texture3D"/>, <see cref="TextureCube"/> and the render targets) derives from it since
/// WP3. <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>/<see cref="Effect"/>/
/// <see cref="SpriteFont"/> still do not: each owns its native handle through its own
/// <c>NativeResourceHandle</c> field and a non-virtual <c>Dispose()</c>, so reparenting them is a
/// behaviour-touching refactor rather than a base-class swap.
///
/// Implemented managed. <c>graphics_resource.h</c> does expose <c>set_name</c>/<c>get_tag</c>/
/// <c>subscribe_disposing</c> and the rest -- a sweep of unbound header functions found them -- and
/// binding <see cref="Name"/> would gain something real: renderers use it as a debug label, so it
/// would show up in a graphics capture. It is not bound because this base does not hold the native
/// handle; its subclasses do, individually, and reaching one from here needs the same reparenting
/// refactor described above. Recorded as a deliberate cost, not as an absence.
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

    /// <summary>A managed-side label, exactly as in real XNA. Not sent to native, which is a
    /// current limitation rather than a property of the concept -- see this type's own doc comment:
    /// <c>cna_graphics_resource_set_name</c> exists and renderers use it as a debug label.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Arbitrary caller-owned payload, exactly as in real XNA. Never read by this project
    /// and deliberately never sent to native -- unlike <see cref="Name"/>, a managed object
    /// reference has nothing meaningful to become on the other side of the ABI.</summary>
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
