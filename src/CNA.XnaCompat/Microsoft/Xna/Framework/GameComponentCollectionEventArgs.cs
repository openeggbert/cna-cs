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
    /// <summary>Public, matching real XNA -- its constructor is public too, and a game that raises
    /// the collection events itself (a test double, for instance) needs to be able to construct
    /// the argument.</summary>
    public GameComponentCollectionEventArgs(CNA.IGameComponent gameComponent)
    {
        ArgumentNullException.ThrowIfNull(gameComponent);
        GameComponent = gameComponent;
    }

    /// <summary>
    /// The component the event is about.
    ///
    /// Named <c>GameComponent</c>, not <c>Component</c>: real XNA calls it <c>GameComponent</c>,
    /// and a handler written against XNA reads <c>e.GameComponent</c>. The first version of this
    /// type used the shorter name, which compiled fine here and would have failed in every ported
    /// game -- a member-level diff against the engine's own headers is what surfaced it.
    ///
    /// Typed as <see cref="CNA.IGameComponent"/> rather than this namespace's own, for the reason
    /// <see cref="GameComponentCollection"/> records about its element type: the collection holds
    /// <c>CNA.GameComponent</c>, which both namespaces' components derive from, and a cast to the
    /// compat interface would fail for a component that only implements the CNA one.
    /// </summary>
    public CNA.IGameComponent GameComponent { get; }
}
