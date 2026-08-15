namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Game</c>. <c>Content</c>/<c>GraphicsDevice</c> are re-exposed with
/// compat-typed return values via covariant-return factory overrides (never touching
/// CNA.Interop -- see docs/architecture.md); <c>Update</c>/<c>Draw</c> get a
/// <see cref="Microsoft.Xna.Framework.GameTime"/>-typed overload for game code to override,
/// bridged from the base <see cref="CNA.Framework.GameTime"/>-typed one, which is sealed here so
/// a game author cannot accidentally override the wrong one. Everything else
/// (<c>Initialize</c>, <c>LoadContent</c>, <c>UnloadContent</c>, <c>Run</c>, <c>Exit</c>,
/// <c>Dispose</c>) is inherited unchanged, since those signatures don't involve any type that
/// differs between CNA.Framework and Microsoft.Xna.Framework.
/// </summary>
public abstract class Game : CNA.Framework.Game
{
    public new Content.ContentManager Content => (Content.ContentManager)base.Content;

    public new Graphics.GraphicsDevice GraphicsDevice => (Graphics.GraphicsDevice)base.GraphicsDevice;

    protected override Content.ContentManager CreateContentManager() =>
        new Content.ContentManager(GetNativeContentHandle());

    protected override Graphics.GraphicsDevice CreateGraphicsDevice() =>
        new Graphics.GraphicsDevice(GetNativeGraphicsDeviceHandle());

    protected sealed override void Update(CNA.Framework.GameTime gameTime) => Update(GameTime.FromFramework(gameTime));

    protected sealed override void Draw(CNA.Framework.GameTime gameTime) => Draw(GameTime.FromFramework(gameTime));

    protected virtual void Update(GameTime gameTime)
    {
    }

    protected virtual void Draw(GameTime gameTime)
    {
    }
}
