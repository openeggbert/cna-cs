namespace CNA.Interop;

/// <summary>
/// The <c>CNA_GRAPHICS_DEVICE_MANAGER_EVENT_*</c> identities
/// (<c>runtime_graphics_manager.h:65-73</c>), which map one-to-one onto XNA's four
/// <c>IGraphicsDeviceService</c> events.
///
/// The header's own type is a bare <c>uint32_t</c> alias rather than a C enum, so the values are
/// spelled out here and each member is passed to native as <c>(uint)</c>. Declaring it as an enum
/// on this side is what stops a caller handing
/// <c>cna_graphics_device_manager_subscribe</c> an arbitrary integer.
///
/// <c>CNA_PreparingDeviceSettingsCallback</c> is deliberately not a member: it is the one manager
/// event carrying data, so it has its own subscribe entry point and a different callback shape.
/// </summary>
internal enum CnaGraphicsDeviceManagerEvent : uint
{
    Disposed = 0,
    DeviceCreated = 1,
    DeviceDisposing = 2,
    DeviceReset = 3,
    DeviceResetting = 4,
}
