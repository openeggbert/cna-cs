namespace CNA.Input;

/// <summary>
/// What kind of physical device a game pad slot represents (steering wheel, dance pad, etc.).
/// Member names and numeric values both confirmed against the real, shipped openeggbert/cna C
/// API's own <c>CNA_GAMEPAD_TYPE_*</c> constants (<c>input_gamepad.h</c>) -- this enum's
/// declaration-order values happened to already match exactly, resolving what was, before this
/// migration, a lower-confidence guess (see <c>NEXT.md</c>'s native-ABI-migration entry, step 11).
/// </summary>
public enum GamePadType
{
    Unknown = 0,
    GamePad = 1,
    Wheel = 2,
    ArcadeStick = 3,
    FlightStick = 4,
    DancePad = 5,
    Guitar = 6,
    AlternateGuitar = 7,
    DrumKit = 8,
    BigButtonPad = 9,
}
