namespace CNA.Interop;

/// <summary>
/// The <c>CNA_GAME_EVENT_*</c> identities (<c>runtime.h</c>), which map onto XNA's
/// <c>Game.Activated</c>/<c>Deactivated</c>/<c>Disposed</c>/<c>Exiting</c>.
///
/// A bare <c>uint32_t</c> alias in the header, so the values are spelled out here -- see
/// <see cref="CnaGraphicsDeviceManagerEvent"/> for why that shape recurs.
/// </summary>
internal enum CnaGameEvent : uint
{
    Activated = 0,
    Deactivated = 1,
    Disposed = 2,
    Exiting = 3,
}
