namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible game component. The public hierarchy is independent from CNA's hierarchy;
/// an internal native-backed adapter owns the CNA component handle and relays callbacks here.
/// </summary>
public class GameComponent : IGameComponent, IUpdateable, IDisposable
{
    private bool _disposed;

    public GameComponent(Game game)
        : this(game, drawable: false)
    {
    }

    private protected GameComponent(Game game, bool drawable)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;
        Inner = drawable ? new DrawableAdapter(this, game) : new UpdateAdapter(this, game);
        Inner.Disposed += OnInnerDisposed;
        Inner.EnabledChanged += (_, eventArgs) => OnEnabledChanged(this, eventArgs);
        Inner.UpdateOrderChanged += (_, eventArgs) => OnUpdateOrderChanged(this, eventArgs);
    }

    ~GameComponent()
    {
        Dispose(false);
    }

    internal CNA.GameComponent Inner { get; }

    public Game Game { get; }

    public bool Enabled
    {
        get => Inner.Enabled;
        set => Inner.Enabled = value;
    }

    public int UpdateOrder
    {
        get => Inner.UpdateOrder;
        set => Inner.UpdateOrder = value;
    }

    public event EventHandler<EventArgs>? Disposed;

    public event EventHandler<EventArgs>? EnabledChanged;

    public event EventHandler<EventArgs>? UpdateOrderChanged;

    public virtual void Initialize()
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Inner.Dispose();
    }

    protected virtual void OnEnabledChanged(object sender, EventArgs args) =>
        EnabledChanged?.Invoke(this, args);

    protected virtual void OnUpdateOrderChanged(object sender, EventArgs args) =>
        UpdateOrderChanged?.Invoke(this, args);

    private void OnInnerDisposed(object? sender, EventArgs args)
    {
        _disposed = true;
        Disposed?.Invoke(this, args);
    }

    private sealed class UpdateAdapter : CNA.GameComponent
    {
        private readonly GameComponent _owner;

        internal UpdateAdapter(GameComponent owner, Game game)
            : base(game.Backend)
        {
            _owner = owner;
        }

        public override void Initialize() => _owner.Initialize();

        public override void Update(CNA.GameTime gameTime) =>
            _owner.Update(GameTime.FromFramework(gameTime));
    }

    private sealed class DrawableAdapter : CNA.DrawableGameComponent
    {
        private readonly GameComponent _owner;

        internal DrawableAdapter(GameComponent owner, Game game)
            : base(game.Backend)
        {
            _owner = owner;
        }

        public override void Initialize() => _owner.Initialize();

        public override void Update(CNA.GameTime gameTime) =>
            _owner.Update(GameTime.FromFramework(gameTime));

        public override void Draw(CNA.GameTime gameTime) =>
            ((DrawableGameComponent)_owner).Draw(GameTime.FromFramework(gameTime));

        protected internal override void LoadContent() =>
            ((DrawableGameComponent)_owner).InvokeLoadContent();

        protected internal override void UnloadContent() =>
            ((DrawableGameComponent)_owner).InvokeUnloadContent();
    }
}
