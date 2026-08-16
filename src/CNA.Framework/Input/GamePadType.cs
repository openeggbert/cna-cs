namespace CNA.Input;

/// <summary>
/// What kind of physical device a game pad slot represents (steering wheel, dance pad, etc.).
/// Member *names* match real XNA; the numeric *values* are declaration-order guesses (0, 1, 2, ...)
/// rather than confirmed real XNA ordinals -- lower confidence than the rest of this file. This
/// only matters if something ever needs to serialize/compare the raw integer value rather than
/// the named enum member (game code almost always does the latter), which nothing in this
/// repository does today.
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
