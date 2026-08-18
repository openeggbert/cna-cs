using CNA.Graphics;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>IGraphicsDeviceService</c>: the service a
/// <see cref="GraphicsDeviceManager"/> registers in <see cref="GameServiceContainer"/> so
/// components can reach the device without depending on the manager directly.
///
/// The four events are the whole point of the contract -- a component that holds device-dependent
/// resources needs to release them on <see cref="DeviceResetting"/> and rebuild on
/// <see cref="DeviceReset"/>.
/// </summary>
public interface IGraphicsDeviceService
{
    GraphicsDevice GraphicsDevice { get; }

    event EventHandler<EventArgs>? DeviceCreated;

    event EventHandler<EventArgs>? DeviceDisposing;

    event EventHandler<EventArgs>? DeviceReset;

    event EventHandler<EventArgs>? DeviceResetting;
}

/// <summary>Matches real XNA's <c>IGraphicsDeviceManager</c>: the contract <see cref="Game"/> uses
/// to drive device creation and per-frame presentation, independent of any particular manager
/// implementation.</summary>
public interface IGraphicsDeviceManager
{
    /// <summary>Returns <see langword="false"/> to tell the game to skip drawing this frame -- for
    /// example while the device is lost.</summary>
    bool BeginDraw();

    void CreateDevice();

    void EndDraw();
}
