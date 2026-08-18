namespace CNA.Interop;

/// <summary>
/// The <c>CNA_GAME_WINDOW_EVENT_*</c> identities (<c>runtime_window.h</c>), which map one-to-one
/// onto XNA's three <c>GameWindow</c> events.
///
/// The header's own type is a bare <c>uint32_t</c> alias, so the values are spelled out here and
/// each member is passed to native as <c>(uint)</c> -- the same shape
/// <see cref="CnaGraphicsDeviceManagerEvent"/> uses, and for the same reason.
/// </summary>
internal enum CnaGameWindowEvent : uint
{
    ClientSizeChanged = 0,
    OrientationChanged = 1,
    ScreenDeviceNameChanged = 2,
}
