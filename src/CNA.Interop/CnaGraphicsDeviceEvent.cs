namespace CNA.Interop;

/// <summary>
/// The <c>CNA_GRAPHICS_DEVICE_EVENT_*</c> identities (<c>graphics_device.h</c>), mapping onto XNA's
/// <c>GraphicsDevice.Disposing</c>/<c>DeviceLost</c>/<c>DeviceReset</c>/<c>DeviceResetting</c>.
///
/// Distinct from <see cref="CnaGraphicsDeviceManagerEvent"/> despite the similar names and
/// overlapping meanings: these are the *device's* own events, released with
/// <c>cna_graphics_device_unsubscribe</c>, while the manager's are released with
/// <c>cna_game_unsubscribe</c>. Mixing the two registration families would release the wrong handle.
///
/// <c>ResourceCreated</c>/<c>ResourceDestroyed</c> are not here -- they carry data and have their
/// own subscribe entry points.
/// </summary>
internal enum CnaGraphicsDeviceEvent : uint
{
    Disposing = 0,
    DeviceLost = 1,
    DeviceReset = 2,
    DeviceResetting = 3,
}
