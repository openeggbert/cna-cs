namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>ResourceCreatedEventArgs</c>: carries the resource
/// <see cref="GraphicsDevice.ResourceCreated"/> is about.
///
/// <see cref="Resource"/> is always <see langword="null"/> here, and that is deliberate rather than
/// unfinished. The native callback carries the resource's handle, and this binding has no way to map
/// a bare handle back to the managed wrapper that owns it -- the limitation
/// <see cref="TextureCollection"/> documents at length. Reporting <see langword="null"/> is honest;
/// inventing a second wrapper around the same handle would be a double-free waiting to happen.
/// </summary>
public class ResourceCreatedEventArgs : EventArgs
{
    public ResourceCreatedEventArgs(object? resource)
    {
        Resource = resource;
    }

    public object? Resource { get; }
}

/// <summary>Matches real XNA's <c>ResourceDestroyedEventArgs</c>. See
/// <see cref="ResourceCreatedEventArgs"/> for why the resource cannot be reported;
/// <see cref="Name"/> is null for the same reason -- the ABI's callback
/// carries a handle, not a name.</summary>
public class ResourceDestroyedEventArgs : EventArgs
{
    public ResourceDestroyedEventArgs(string? name, object? tag)
    {
        Name = name;
        Tag = tag;
    }

    public string? Name { get; }

    public object? Tag { get; }
}
