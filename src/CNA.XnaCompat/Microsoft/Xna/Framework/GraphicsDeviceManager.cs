namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GraphicsDeviceManager</c>. Mostly a pure subclass -- the
/// int/bool preferences and <c>ApplyChanges</c>/<c>ToggleFullScreen</c> are inherited unchanged
/// from <see cref="CNA.GraphicsDeviceManager"/>; only the members whose enum or device types
/// differ per namespace need re-typing.</summary>
public class GraphicsDeviceManager : CNA.GraphicsDeviceManager, IGraphicsDeviceService, IGraphicsDeviceManager
{
    public GraphicsDeviceManager(Game game)
        : base(game)
    {
    }

    public new Graphics.GraphicsDevice GraphicsDevice => (Graphics.GraphicsDevice)base.GraphicsDevice;

    public new Graphics.SurfaceFormat PreferredBackBufferFormat
    {
        get => (Graphics.SurfaceFormat)(int)base.PreferredBackBufferFormat;
        set => base.PreferredBackBufferFormat = (CNA.Graphics.SurfaceFormat)(int)value;
    }

    public new Graphics.DepthFormat PreferredDepthStencilFormat
    {
        get => (Graphics.DepthFormat)(int)base.PreferredDepthStencilFormat;
        set => base.PreferredDepthStencilFormat = (CNA.Graphics.DepthFormat)(int)value;
    }

    public new Graphics.GraphicsProfile GraphicsProfile
    {
        get => (Graphics.GraphicsProfile)(int)base.GraphicsProfile;
        set => base.GraphicsProfile = (CNA.Graphics.GraphicsProfile)(int)value;
    }

    public new DisplayOrientation SupportedOrientations
    {
        get => (DisplayOrientation)(int)base.SupportedOrientations;
        set => base.SupportedOrientations = (CNA.DisplayOrientation)(int)value;
    }

    /// <summary>The compat <see cref="IGraphicsDeviceService"/> contract, satisfied by the same
    /// object that already satisfies the CNA one -- registered into <c>Game.Services</c> by
    /// the base constructor, so a component can look up either. The events forward to the base's,
    /// which are documented as inert until the native subscription route is bound.</summary>
    Graphics.GraphicsDevice IGraphicsDeviceService.GraphicsDevice => GraphicsDevice;

    event EventHandler<EventArgs>? IGraphicsDeviceService.DeviceCreated
    {
        add => base.DeviceCreated += value;
        remove => base.DeviceCreated -= value;
    }

    event EventHandler<EventArgs>? IGraphicsDeviceService.DeviceDisposing
    {
        add => base.DeviceDisposing += value;
        remove => base.DeviceDisposing -= value;
    }

    event EventHandler<EventArgs>? IGraphicsDeviceService.DeviceReset
    {
        add => base.DeviceReset += value;
        remove => base.DeviceReset -= value;
    }

    event EventHandler<EventArgs>? IGraphicsDeviceService.DeviceResetting
    {
        add => base.DeviceResetting += value;
        remove => base.DeviceResetting -= value;
    }

}
