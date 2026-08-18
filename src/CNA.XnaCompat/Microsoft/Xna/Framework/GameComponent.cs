namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GameComponent</c>. A pure subclass -- <c>Enabled</c>/
/// <c>UpdateOrder</c>/<c>Initialize</c>/<c>Dispose</c> are inherited unchanged; only
/// <c>Update</c> needs a <see cref="GameTime"/>-typed overload, bridged from the base's,
/// which is sealed here so a game author cannot accidentally override the wrong one -- the same
/// pattern <see cref="Game"/> itself uses.</summary>
public class GameComponent : CNA.GameComponent, IGameComponent, IUpdateable
{
    public GameComponent(Game game)
        : base(game)
    {
    }

    public sealed override void Update(CNA.GameTime gameTime) => Update(GameTime.FromFramework(gameTime));

    public virtual void Update(GameTime gameTime)
    {
    }
}
