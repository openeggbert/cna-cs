namespace Microsoft.Xna.Framework;

/// <summary>Provides the component involved in a game-component collection change.</summary>
public class GameComponentCollectionEventArgs : EventArgs
{
    public GameComponentCollectionEventArgs(IGameComponent gameComponent)
    {
        ArgumentNullException.ThrowIfNull(gameComponent);
        GameComponent = gameComponent;
    }

    public IGameComponent GameComponent { get; }
}
