namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA's game facade. The private <see cref="BackendGame"/> owns CNA's native callback and
/// lifetime machinery; this type owns the public XNA hierarchy and callback contract.
/// </summary>
public class Game : IDisposable
{
    private readonly BackendGame _backend;
    private Content.ContentManager _content;
    private GameComponentCollection? _components;
    private GameWindow? _window;
    private GraphicsDeviceManager? _graphicsDeviceManager;
    private bool _disposed;

    public Game()
    {
        Services = new GameServiceContainer();
        _backend = new BackendGame(this);
        _content = new Content.ContentManager(_backend.Content, Services);

        _backend.Activated += (_, args) => OnActivated(this, args);
        _backend.Deactivated += (_, args) => OnDeactivated(this, args);
        _backend.Exiting += (_, args) => OnExiting(this, args);
        _backend.Disposed += (_, args) => Disposed?.Invoke(this, args);
    }

    internal CNA.Game Backend => _backend;

    /// <summary>
    /// The XNA manager is owned by its game for native-lifetime purposes. CNA's native game tears
    /// down devices while it is being destroyed; dispose the facade's subscriptions before that
    /// boundary so native can never call a freed managed event context during game destruction.
    /// </summary>
    internal void RegisterGraphicsDeviceManager(GraphicsDeviceManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (_graphicsDeviceManager is not null && !ReferenceEquals(_graphicsDeviceManager, manager))
        {
            throw new ArgumentException("A Game may have only one GraphicsDeviceManager.", nameof(manager));
        }

        _graphicsDeviceManager = manager;
    }

    internal void UnregisterGraphicsDeviceManager(GraphicsDeviceManager manager)
    {
        if (ReferenceEquals(_graphicsDeviceManager, manager))
        {
            _graphicsDeviceManager = null;
        }
    }

    public GameComponentCollection Components =>
        _components ??= new GameComponentCollection(this, _backend.Components);

    public Content.ContentManager Content
    {
        get => _content;
        set => _content = value ?? throw new ArgumentNullException(nameof(value));
    }

    public Graphics.GraphicsDevice GraphicsDevice =>
        _backend.CompatGraphicsDevice ?? throw new InvalidOperationException(
            "The graphics device is not available until the game has initialized.");

    public TimeSpan InactiveSleepTime
    {
        get => _backend.InactiveSleepTime;
        set => _backend.InactiveSleepTime = value;
    }

    public bool IsActive => _backend.IsActive;

    public bool IsFixedTimeStep
    {
        get => _backend.IsFixedTimeStep;
        set => _backend.IsFixedTimeStep = value;
    }

    public bool IsMouseVisible
    {
        get => _backend.IsMouseVisible;
        set => _backend.IsMouseVisible = value;
    }

    public LaunchParameters LaunchParameters => new(_backend.LaunchParameters);

    public GameServiceContainer Services { get; }

    public TimeSpan TargetElapsedTime
    {
        get => _backend.TargetElapsedTime;
        set => _backend.TargetElapsedTime = value;
    }

    public GameWindow Window => _window ??= new NativeGameWindow(_backend.Window);

    public event EventHandler<EventArgs>? Activated;

    public event EventHandler<EventArgs>? Deactivated;

    public event EventHandler<EventArgs>? Disposed;

    public event EventHandler<EventArgs>? Exiting;

    public void ResetElapsedTime() => _backend.ResetElapsedTime();

    public void Run()
    {
        BeginRun();
        try
        {
            _backend.Run();
        }
        finally
        {
            EndRun();
        }
    }

    public void RunOneFrame() => _backend.RunOneFrame();

    public void Exit() => _backend.Exit();

    public void SuppressDraw() => _backend.SuppressDraw();

    public void Tick() => _backend.Tick();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Game()
    {
        Dispose(false);
    }

    protected virtual bool BeginDraw() => true;

    protected virtual void BeginRun()
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (disposing)
            {
                try
                {
                    _content?.Dispose();
                }
                finally
                {
                    try
                    {
                        _backend?.DisposeCompatGraphicsDevice();
                    }
                    finally
                    {
                        _graphicsDeviceManager?.DisposeFromGame();
                    }
                }
            }
        }
        finally
        {
            // A managed disposal handler may throw, but native game teardown must still run.
            _backend?.Dispose();
        }
    }

    protected virtual void Update(GameTime gameTime)
    {
        FrameworkDispatcher.Update();
    }

    protected virtual void Draw(GameTime gameTime)
    {
    }

    protected virtual void EndDraw()
    {
    }

    protected virtual void EndRun()
    {
    }

    protected virtual void Initialize()
    {
    }

    protected virtual void LoadContent()
    {
    }

    protected virtual void OnActivated(object sender, EventArgs args) => Activated?.Invoke(sender, args);

    protected virtual void OnDeactivated(object sender, EventArgs args) => Deactivated?.Invoke(sender, args);

    protected virtual void OnExiting(object sender, EventArgs args) => Exiting?.Invoke(sender, args);

    protected virtual bool ShowMissingRequirementMessage(Exception exception) => false;

    protected virtual void UnloadContent()
    {
    }

    private void OnInitializeFromBackend()
    {
        _ = GraphicsDevice;
        Initialize();
    }

    private void OnDrawFromBackend(GameTime gameTime)
    {
        if (!BeginDraw())
        {
            return;
        }

        try
        {
            Draw(gameTime);
        }
        finally
        {
            EndDraw();
        }
    }

    private sealed class BackendGame : CNA.Game
    {
        private readonly Game _owner;
        private Graphics.GraphicsDevice? _compatGraphicsDevice;

        internal BackendGame(Game owner)
        {
            _owner = owner;
        }

        internal Graphics.GraphicsDevice? CompatGraphicsDevice => _compatGraphicsDevice;

        internal void DisposeCompatGraphicsDevice()
        {
            _compatGraphicsDevice?.DisposeFromOwningGame();
            _compatGraphicsDevice = null;
        }

        protected override CNA.Graphics.GraphicsDevice CreateGraphicsDevice() =>
            (_compatGraphicsDevice ??= new Graphics.GraphicsDevice(NativeHandle)).Framework;

        protected override void Initialize() => _owner.OnInitializeFromBackend();

        protected override void LoadContent() => _owner.LoadContent();

        protected override void Update(CNA.GameTime gameTime) =>
            _owner.Update(GameTime.FromFramework(gameTime));

        protected override void Draw(CNA.GameTime gameTime) =>
            _owner.OnDrawFromBackend(GameTime.FromFramework(gameTime));

        protected override void UnloadContent() => _owner.UnloadContent();
    }

    private sealed class NativeGameWindow : GameWindow
    {
        private readonly CNA.GameWindow _backend;

        internal NativeGameWindow(CNA.GameWindow backend)
        {
            _backend = backend;
            Attach(backend);
            backend.ClientSizeChanged += (_, _) => OnClientSizeChanged();
            backend.OrientationChanged += (_, _) => OnOrientationChanged();
            backend.ScreenDeviceNameChanged += (_, _) => OnScreenDeviceNameChanged();
        }

        public override bool AllowUserResizing
        {
            get => _backend.AllowUserResizing;
            set => _backend.AllowUserResizing = value;
        }

        public override Rectangle ClientBounds => _backend.ClientBounds.ToCompat();

        public override DisplayOrientation CurrentOrientation =>
            (DisplayOrientation)(int)_backend.CurrentOrientation;

        public override IntPtr Handle => _backend.Handle;

        public override string ScreenDeviceName => _backend.ScreenDeviceName;

        public override void BeginScreenDeviceChange(bool willBeFullScreen) =>
            _backend.BeginScreenDeviceChange(willBeFullScreen);

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight) =>
            _backend.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
        }

        protected override void SetTitle(string title) => _backend.Title = title;
    }
}
