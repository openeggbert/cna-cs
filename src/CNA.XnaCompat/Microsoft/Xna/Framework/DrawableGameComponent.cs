namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>DrawableGameComponent</c>. See <see cref="GameComponent"/> for
/// the sealed-bridge pattern; the same applies to <c>Draw</c>.</summary>
public class DrawableGameComponent : CNA.DrawableGameComponent, IGameComponent, IUpdateable, IDrawable
{
    public DrawableGameComponent(Game game)
        : base(game)
    {
    }

    public new Graphics.GraphicsDevice GraphicsDevice => (Graphics.GraphicsDevice)base.GraphicsDevice;

    public sealed override void Update(CNA.GameTime gameTime) => Update(GameTime.FromFramework(gameTime));

    public virtual void Update(GameTime gameTime)
    {
    }

    public sealed override void Draw(CNA.GameTime gameTime) => Draw(GameTime.FromFramework(gameTime));

    public virtual void Draw(GameTime gameTime)
    {
    }
}
