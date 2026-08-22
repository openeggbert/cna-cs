namespace Microsoft.Xna.Framework.Input;

/// <summary>XNA 4.0 game-pad type ordinals. <c>BigButtonPad</c> intentionally differs from CNA's
/// compact native representation and is translated at the facade boundary.</summary>
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
    BigButtonPad = 0x300,
}

internal static class GamePadTypeConversions
{
    internal static GamePadType ToCompat(this CNA.Input.GamePadType value) =>
        value == CNA.Input.GamePadType.BigButtonPad
            ? GamePadType.BigButtonPad
            : (GamePadType)(int)value;
}
