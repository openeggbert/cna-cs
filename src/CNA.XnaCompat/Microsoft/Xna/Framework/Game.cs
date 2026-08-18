namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Game</c>. <c>Content</c>/<c>GraphicsDevice</c> are re-exposed with
/// compat-typed return values via covariant-return factory overrides (never touching
/// CNA.Interop -- see docs/architecture.md); <c>Update</c>/<c>Draw</c> get a
/// <see cref="Microsoft.Xna.Framework.GameTime"/>-typed overload for game code to override,
/// bridged from the base <see cref="CNA.GameTime"/>-typed one, which is sealed here so
/// a game author cannot accidentally override the wrong one. Everything else
/// (<c>Initialize</c>, <c>LoadContent</c>, <c>UnloadContent</c>, <c>Run</c>, <c>Exit</c>,
/// <c>Dispose</c>) is inherited unchanged, since those signatures don't involve any type that
/// differs between CNA and Microsoft.Xna.Framework.
/// </summary>
public abstract class Game : CNA.Game
{
    public new Content.ContentManager Content => (Content.ContentManager)base.Content;

    public new Graphics.GraphicsDevice GraphicsDevice => (Graphics.GraphicsDevice)base.GraphicsDevice;

    public new GameWindow Window => (GameWindow)base.Window;

    private GameComponentCollection? _components;

    /// <summary>Re-typed only so the name resolves in this namespace -- the element type is the
    /// base <see cref="CNA.GameComponent"/>, which both namespaces' components derive from, so no
    /// element conversion is involved.</summary>
    public new GameComponentCollection Components => _components ??= new GameComponentCollection(base.Components);

    /// <summary>Re-typed so a compat game gets this namespace's own <see cref="LaunchParameters"/>.
    /// The base builds a fresh one per read from the process command line -- see
    /// <see cref="CNA.Game.LaunchParameters"/> for why it cannot come from native -- so re-wrapping
    /// costs nothing beyond the parse that was already happening.</summary>
    public new LaunchParameters LaunchParameters => new(Environment.GetCommandLineArgs().Skip(1));

    protected override Content.ContentManager CreateContentManager() =>
        new Content.ContentManager(GetNativeContentHandle());

    /// <summary>The <em>game</em> handle, for the reason
    /// <see cref="CNA.Game.CreateGraphicsDevice"/> spells out: the device wrapper re-resolves its
    /// device from a game handle on every call, so handing it a device handle breaks every
    /// graphics call. This override carried the same bug as the base and is fixed the same way --
    /// worth stating, because the two are easy to fix independently and leave compat broken.</summary>
    protected override Graphics.GraphicsDevice CreateGraphicsDevice() =>
        new Graphics.GraphicsDevice(NativeHandle);

    protected override CNA.GameWindow CreateWindow() => new GameWindow(NativeHandle);

    protected sealed override void Update(CNA.GameTime gameTime) => Update(GameTime.FromFramework(gameTime));

    protected sealed override void Draw(CNA.GameTime gameTime) => Draw(GameTime.FromFramework(gameTime));

    protected virtual void Update(GameTime gameTime)
    {
    }

    protected virtual void Draw(GameTime gameTime)
    {
    }
}
