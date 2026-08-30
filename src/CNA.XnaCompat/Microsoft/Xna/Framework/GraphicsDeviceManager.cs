namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA's graphics-device manager. The native manager remains private; publishing it as a base
/// class previously made every XNA game inherit CNA-only members and interfaces.
/// </summary>
public class GraphicsDeviceManager : Graphics.IGraphicsDeviceService, IGraphicsDeviceManager, IDisposable
{
    /// <summary>Minimum default back-buffer width used by the XNA device manager.</summary>
    public static readonly int DefaultBackBufferWidth = 800;

    /// <summary>Minimum default back-buffer height used by the XNA device manager.</summary>
    public static readonly int DefaultBackBufferHeight = 480;

    private readonly CNA.GraphicsDeviceManager _backend;

    /// <summary>The CNA manager behind this facade, for <c>CNA.XnaCompat.Extensions</c>. Internal
    /// for the same reason <c>GraphicsDevice.Framework</c> is: the strict facade's public surface is
    /// checked member for member against XNA's own metadata.</summary>
    internal CNA.GraphicsDeviceManager Framework => _backend;
    private bool _disposed;
    private EventHandler<PreparingDeviceSettingsEventArgs>? _preparingDeviceSettings;

    public GraphicsDeviceManager(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;
        _backend = new CNA.GraphicsDeviceManager(game.Backend);
        _backend.DeviceCreated += (_, args) => OnDeviceCreated(this, args);
        _backend.DeviceDisposing += (_, args) => OnDeviceDisposing(this, args);
        _backend.DeviceReset += (_, args) => OnDeviceReset(this, args);
        _backend.DeviceResetting += (_, args) => OnDeviceResetting(this, args);
        _backend.PreparingDeviceSettings += OnPreparingDeviceSettings;
        game.RegisterGraphicsDeviceManager(this);
        game.Services.AddService(typeof(IGraphicsDeviceManager), this);
        game.Services.AddService(typeof(Graphics.IGraphicsDeviceService), this);
    }

    internal Game Game { get; }

    public int PreferredBackBufferWidth
    {
        get => _backend.PreferredBackBufferWidth;
        set => _backend.PreferredBackBufferWidth = value;
    }

    public int PreferredBackBufferHeight
    {
        get => _backend.PreferredBackBufferHeight;
        set => _backend.PreferredBackBufferHeight = value;
    }

    public Graphics.SurfaceFormat PreferredBackBufferFormat
    {
        get => (Graphics.SurfaceFormat)(int)_backend.PreferredBackBufferFormat;
        set => _backend.PreferredBackBufferFormat = (CNA.Graphics.SurfaceFormat)(int)value;
    }

    public Graphics.DepthFormat PreferredDepthStencilFormat
    {
        get => (Graphics.DepthFormat)(int)_backend.PreferredDepthStencilFormat;
        set => _backend.PreferredDepthStencilFormat = (CNA.Graphics.DepthFormat)(int)value;
    }

    public bool IsFullScreen
    {
        get => _backend.IsFullScreen;
        set => _backend.IsFullScreen = value;
    }

    public bool PreferMultiSampling
    {
        get => _backend.PreferMultiSampling;
        set => _backend.PreferMultiSampling = value;
    }

    public bool SynchronizeWithVerticalRetrace
    {
        get => _backend.SynchronizeWithVerticalRetrace;
        set => _backend.SynchronizeWithVerticalRetrace = value;
    }

    public Graphics.GraphicsProfile GraphicsProfile
    {
        get => (Graphics.GraphicsProfile)(int)_backend.GraphicsProfile;
        set => _backend.GraphicsProfile = (CNA.Graphics.GraphicsProfile)(int)value;
    }

    public DisplayOrientation SupportedOrientations
    {
        get => (DisplayOrientation)(int)_backend.SupportedOrientations;
        set => _backend.SupportedOrientations = (CNA.DisplayOrientation)(int)value;
    }

    public Graphics.GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    public event EventHandler<PreparingDeviceSettingsEventArgs>? PreparingDeviceSettings
    {
        add => _preparingDeviceSettings += value;
        remove => _preparingDeviceSettings -= value;
    }

    public event EventHandler<EventArgs>? DeviceCreated;

    public event EventHandler<EventArgs>? DeviceDisposing;

    public event EventHandler<EventArgs>? DeviceReset;

    public event EventHandler<EventArgs>? DeviceResetting;

    public event EventHandler<EventArgs>? Disposed;

    public void ApplyChanges() => _backend.ApplyChanges();

    public void ToggleFullScreen() => _backend.ToggleFullScreen();

    protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
    {
        ArgumentNullException.ThrowIfNull(newDeviceInfo);
        return !GraphicsDevice.IsDisposed && GraphicsDevice.GraphicsProfile == newDeviceInfo.GraphicsProfile;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _disposed = true;
        _backend.Dispose();
        if (ReferenceEquals(Game.Services.GetService(typeof(IGraphicsDeviceManager)), this))
        {
            Game.Services.RemoveService(typeof(IGraphicsDeviceManager));
        }

        if (ReferenceEquals(Game.Services.GetService(typeof(Graphics.IGraphicsDeviceService)), this))
        {
            Game.Services.RemoveService(typeof(Graphics.IGraphicsDeviceService));
        }

        Game.UnregisterGraphicsDeviceManager(this);
        Disposed?.Invoke(this, EventArgs.Empty);
    }

    internal void DisposeFromGame() => Dispose(true);

    protected virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
    {
        var information = new GraphicsDeviceInformation
        {
            Adapter = Graphics.GraphicsAdapter.DefaultAdapter,
            GraphicsProfile = GraphicsProfile,
            PresentationParameters = new Graphics.PresentationParameters
            {
                BackBufferWidth = PreferredBackBufferWidth,
                BackBufferHeight = PreferredBackBufferHeight,
                BackBufferFormat = PreferredBackBufferFormat,
                DepthStencilFormat = PreferredDepthStencilFormat,
                IsFullScreen = IsFullScreen,
            },
        };

        _ = anySuitableDevice;
        return information;
    }

    protected virtual void OnDeviceCreated(object sender, EventArgs args) =>
        DeviceCreated?.Invoke(sender, args);

    protected virtual void OnDeviceDisposing(object sender, EventArgs args) =>
        DeviceDisposing?.Invoke(sender, args);

    protected virtual void OnDeviceReset(object sender, EventArgs args) =>
        DeviceReset?.Invoke(sender, args);

    protected virtual void OnDeviceResetting(object sender, EventArgs args) =>
        DeviceResetting?.Invoke(sender, args);

    protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args) =>
        _preparingDeviceSettings?.Invoke(sender, args);

    protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
    {
        ArgumentNullException.ThrowIfNull(foundDevices);
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    bool IGraphicsDeviceManager.BeginDraw() => _backend.BeginDraw();

    void IGraphicsDeviceManager.CreateDevice() => _backend.CreateDevice();

    void IGraphicsDeviceManager.EndDraw() => _backend.EndDraw();

    private void OnPreparingDeviceSettings(object? sender, CNA.PreparingDeviceSettingsEventArgs args)
    {
        GraphicsDeviceInformation information = GraphicsDeviceInformation.FromFramework(args.GraphicsDeviceInformation);
        OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(information));
        information.CopyTo(args.GraphicsDeviceInformation);
    }
}
