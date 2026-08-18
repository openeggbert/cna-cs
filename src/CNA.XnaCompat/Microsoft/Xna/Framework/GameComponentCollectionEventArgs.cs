namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>GameComponentCollectionEventArgs</c>: carries the component a
/// <see cref="GameComponentCollection.ComponentAdded"/> or
/// <see cref="GameComponentCollection.ComponentRemoved"/> event is about.
///
/// A distinct type from <see cref="CNA.GameComponentCollectionEventArgs"/>, not an alias, because
/// its <see cref="GameComponent"/> is typed on this namespace's own
/// <see cref="IGameComponent"/> -- which is itself distinct, since <c>IUpdateable</c>/
/// <c>IDrawable</c> take this namespace's <c>GameTime</c>.
///
/// Both this type and the two events that use it were missing until the WP16 re-audit: the compat
/// collection forwarded every member except the events, so a compat game had no way to observe its
/// own component list changing.
/// </summary>
public class GameComponentCollectionEventArgs : EventArgs
{
    internal GameComponentCollectionEventArgs(CNA.IGameComponent gameComponent)
    {
        ArgumentNullException.ThrowIfNull(gameComponent);
        Component = gameComponent;
    }

    /// <summary>
    /// The component the event is about.
    ///
    /// Typed as <see cref="CNA.IGameComponent"/> rather than this namespace's own, for the reason
    /// <see cref="GameComponentCollection"/> records about its element type: the collection holds
    /// <c>CNA.GameComponent</c>, which both namespaces' components derive from, and a cast to the
    /// compat interface would fail for a component that only implements the CNA one.
    /// </summary>
    public CNA.IGameComponent Component { get; }
}
