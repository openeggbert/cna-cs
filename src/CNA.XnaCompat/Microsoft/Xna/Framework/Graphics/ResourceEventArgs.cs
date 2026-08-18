namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Matches real XNA's <c>ResourceCreatedEventArgs</c>: reports the resource a
/// <c>GraphicsDevice</c> just created, through its <c>ResourceCreated</c> event.</summary>
public class ResourceCreatedEventArgs : EventArgs
{
    internal ResourceCreatedEventArgs(object resource)
    {
        Resource = resource;
    }

    /// <summary>Typed <see cref="object"/>, as in real XNA -- the event reports every resource
    /// kind, which share no common base narrower than this once
    /// <see cref="Microsoft.Xna.Framework.Graphics.GraphicsResource"/> and the buffer types are
    /// both included.</summary>
    public object Resource { get; }
}

/// <summary>Matches real XNA's <c>ResourceDestroyedEventArgs</c>. Reports the resource's
/// <see cref="Name"/> and <see cref="Tag"/> rather than the object itself, exactly as XNA does --
/// by the time the event fires the resource is gone, so handing out a reference to it would invite
/// use-after-dispose.</summary>
public class ResourceDestroyedEventArgs : EventArgs
{
    internal ResourceDestroyedEventArgs(string name, object? tag)
    {
        Name = name;
        Tag = tag;
    }

    public string Name { get; }

    public object? Tag { get; }
}
