using CNA.Graphics;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GraphicsDeviceManager</c>: the object a game constructs to state its
/// back-buffer, full-screen and profile preferences, then applies with
/// <see cref="ApplyChanges"/>.
///
/// Phase 8 WP6 replaced a placeholder that had only a <see cref="Game"/> property (everything else
/// was recorded as deferred) with a real binding over <c>runtime_graphics_manager.h</c>.
///
/// The constructor really does create the native manager, rather than deferring: the C API
/// documents <c>cna_graphics_device_manager_create</c> as registering the new manager as the
/// game's graphics device manager *and* its graphics device service, and as refusing a second
/// manager for the same game. That is exactly real XNA's own
/// <c>new GraphicsDeviceManager(this)</c> contract, so the two map one-to-one and a second
/// construction fails loudly here just as it does there.
///
/// Every property is a live round trip to native, not a cached value -- so a preference read back
/// after <see cref="ApplyChanges"/> reports what the engine actually settled on, which may differ
/// from what was asked for (a requested back-buffer size the display cannot provide, for example).
/// Real XNA behaves the same way.
/// </summary>
public class GraphicsDeviceManager : IGraphicsDeviceService, IGraphicsDeviceManager, IDisposable
{
    private readonly NativeResourceHandle _handle;

    public GraphicsDeviceManager(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;

        CnaResult result = Native.cna_graphics_device_manager_create(new CnaHandle(game.NativeHandle), out CnaHandle manager);
        CnaException.ThrowIfFailed(result, nameof(GraphicsDeviceManager));

        _handle = new NativeResourceHandle(manager.AsNint, ReleaseNative);

        // Real XNA's GraphicsDeviceManager registers itself as the game's IGraphicsDeviceService
        // from its own constructor, which is how components find the device without depending on
        // the manager type. The C API's create call does the same registration natively (its own
        // doc calls it "the game's graphics device manager and graphics device service"), so doing
        // it here keeps the managed service container agreeing with native rather than being a
        // second, separate registry.
        game.Services.AddService(typeof(IGraphicsDeviceService), this);
    }

    public Game Game { get; }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    private static void ReleaseNative(nint handleValue) => Native.cna_graphics_device_manager_destroy(new CnaHandle(handleValue));

    public int PreferredBackBufferWidth
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_preferred_back_buffer_width(NativeHandle, out int value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferWidth));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_preferred_back_buffer_width(NativeHandle, value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferWidth));
        }
    }

    public int PreferredBackBufferHeight
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_preferred_back_buffer_height(NativeHandle, out int value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferHeight));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_preferred_back_buffer_height(NativeHandle, value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferHeight));
        }
    }

    public SurfaceFormat PreferredBackBufferFormat
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_preferred_back_buffer_format(NativeHandle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferFormat));
            return (SurfaceFormat)value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_preferred_back_buffer_format(NativeHandle, (uint)value);
            CnaException.ThrowIfFailed(result, nameof(PreferredBackBufferFormat));
        }
    }

    public DepthFormat PreferredDepthStencilFormat
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_preferred_depth_stencil_format(NativeHandle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(PreferredDepthStencilFormat));
            return (DepthFormat)value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_preferred_depth_stencil_format(NativeHandle, (uint)value);
            CnaException.ThrowIfFailed(result, nameof(PreferredDepthStencilFormat));
        }
    }

    public bool IsFullScreen
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_is_full_screen(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsFullScreen));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_is_full_screen(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(IsFullScreen));
        }
    }

    public bool PreferMultiSampling
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_prefer_multi_sampling(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(PreferMultiSampling));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_prefer_multi_sampling(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(PreferMultiSampling));
        }
    }

    public bool SynchronizeWithVerticalRetrace
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_synchronize_with_vertical_retrace(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(SynchronizeWithVerticalRetrace));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_synchronize_with_vertical_retrace(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(SynchronizeWithVerticalRetrace));
        }
    }

    public GraphicsProfile GraphicsProfile
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_graphics_profile(NativeHandle, out CnaGraphicsProfile value);
            CnaException.ThrowIfFailed(result, nameof(GraphicsProfile));
            return (GraphicsProfile)value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_graphics_profile(NativeHandle, (CnaGraphicsProfile)value);
            CnaException.ThrowIfFailed(result, nameof(GraphicsProfile));
        }
    }

    public DisplayOrientation SupportedOrientations
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_manager_get_supported_orientations(NativeHandle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(SupportedOrientations));
            return (DisplayOrientation)value;
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_manager_set_supported_orientations(NativeHandle, (uint)value);
            CnaException.ThrowIfFailed(result, nameof(SupportedOrientations));
        }
    }

    /// <summary>Applies every preference set since the last call. Until this runs, the properties
    /// above are only requests -- matching real XNA, where setting
    /// <see cref="PreferredBackBufferWidth"/> alone changes nothing.</summary>
    public void ApplyChanges()
    {
        CnaResult result = Native.cna_graphics_device_manager_apply_changes(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(ApplyChanges));
    }

    public void ToggleFullScreen()
    {
        CnaResult result = Native.cna_graphics_device_manager_toggle_full_screen(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(ToggleFullScreen));
    }

    /// <summary>The device this manager configures. Deliberately reads
    /// <see cref="CNA.Game.GraphicsDevice"/> rather than
    /// <c>cna_graphics_device_manager_get_graphics_device</c>: the game already owns the single
    /// managed <see cref="Graphics.GraphicsDevice"/> wrapper for this game, and building a second
    /// wrapper around the same native device would give callers two objects whose cached state
    /// (<c>BlendState</c>, <c>Indices</c>, the sampler/texture collections) could disagree.</summary>
    public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    // CS0067 ("event is never used") is expected and correct here: these four are deliberately
    // inert until the native subscription route is bound. Suppressed with a pragma rather than
    // silenced by a never-called raiser method, so the compiler keeps telling the truth about them
    // and the reason lives next to the suppression.
#pragma warning disable CS0067

    /// <summary>Raised after the device is created. Never fires today: device creation happens
    /// inside native <c>cna_graphics_device_manager_create_device</c>, and the C API's own
    /// subscription route (<c>cna_graphics_device_manager_subscribe</c>) is not bound yet -- see
    /// <c>plan.md</c> WP15. The events exist so the <see cref="IGraphicsDeviceService"/> contract
    /// is real and XNA source that subscribes compiles; they are documented as inert rather than
    /// omitted.</summary>
    public event EventHandler<EventArgs>? DeviceCreated;

    public event EventHandler<EventArgs>? DeviceDisposing;

    public event EventHandler<EventArgs>? DeviceReset;

    public event EventHandler<EventArgs>? DeviceResetting;

#pragma warning restore CS0067

    /// <summary>Matches <c>IGraphicsDeviceManager.BeginDraw</c>: <see langword="false"/> tells the
    /// game to skip this frame's drawing.</summary>
    public bool BeginDraw()
    {
        CnaResult result = Native.cna_graphics_device_manager_begin_draw(NativeHandle, out byte shouldDraw);
        CnaException.ThrowIfFailed(result, nameof(BeginDraw));
        return shouldDraw != 0;
    }

    public void CreateDevice()
    {
        CnaResult result = Native.cna_graphics_device_manager_create_device(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(CreateDevice));
    }

    public void EndDraw()
    {
        CnaResult result = Native.cna_graphics_device_manager_end_draw(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(EndDraw));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => _handle.Dispose();
}
