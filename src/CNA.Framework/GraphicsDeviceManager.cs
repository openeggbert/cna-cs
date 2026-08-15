namespace CNA.Framework;

/// <summary>
/// Placeholder for the XNA-style graphics configuration manager. <see cref="Game.GraphicsDevice"/>
/// is resolved lazily by <see cref="Game"/> itself, not by this class -- see docs/architecture.md.
/// Backbuffer size/format preferences and <c>ApplyChanges()</c> are Phase 4 (plan.md); this
/// currently exists so <c>new GraphicsDeviceManager(this)</c> in a game constructor compiles and
/// matches the shape every XNA game expects.
/// </summary>
public class GraphicsDeviceManager
{
    public Game Game { get; }

    public GraphicsDeviceManager(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;
    }
}
