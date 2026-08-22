namespace Microsoft.Xna.Framework.GamerServices;

/// <summary>
/// XNA's Gamer Services component surface. The historical service is unavailable on CNA, but the
/// component remains source-compatible and participates in the ordinary game-component lifecycle.
/// </summary>
public class GamerServicesComponent : GameComponent
{
    public GamerServicesComponent(Game game)
        : base(game)
    {
    }

    public override void Initialize() => base.Initialize();

    public override void Update(GameTime gameTime)
    {
    }
}
