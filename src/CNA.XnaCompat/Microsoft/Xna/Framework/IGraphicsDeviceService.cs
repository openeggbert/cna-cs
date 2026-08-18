namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>IGraphicsDeviceService</c>. A distinct interface from
/// <see cref="CNA.IGraphicsDeviceService"/> rather than an alias, because
/// <see cref="Graphics.GraphicsDevice"/> differs per namespace. <c>GraphicsDeviceManager</c>
/// implements both, so a component looking up either contract in
/// <c>Game.Services</c> finds it.</summary>
public interface IGraphicsDeviceService
{
    Graphics.GraphicsDevice GraphicsDevice { get; }

    event EventHandler<EventArgs>? DeviceCreated;

    event EventHandler<EventArgs>? DeviceDisposing;

    event EventHandler<EventArgs>? DeviceReset;

    event EventHandler<EventArgs>? DeviceResetting;
}

/// <summary>XNA 4.0-compatible <c>IGraphicsDeviceManager</c>. Every member involves only
/// <see cref="bool"/> and <see langword="void"/>, so unlike
/// <see cref="IGraphicsDeviceService"/> this could have been inherited -- it is declared here
/// anyway so the type name resolves in this namespace, and
/// <see cref="CNA.IGraphicsDeviceManager"/> is implemented alongside it.</summary>
public interface IGraphicsDeviceManager
{
    bool BeginDraw();

    void CreateDevice();

    void EndDraw();
}
