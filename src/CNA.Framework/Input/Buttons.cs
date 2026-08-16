namespace CNA.Framework.Input;

/// <summary>
/// Bit assignments match real XNA's <c>Buttons</c> flags enum for the core button set (D-pad,
/// Start/Back, stick clicks, shoulders, face buttons). XNA's additional flags for representing
/// thumbstick directions and trigger pulls as pseudo-buttons are not included -- use
/// <see cref="GamePadState.ThumbSticks"/> / <see cref="GamePadState.Triggers"/> directly instead;
/// see plan.md Phase 4.
/// </summary>
[Flags]
public enum Buttons : uint
{
    DPadUp = 1,
    DPadDown = 2,
    DPadLeft = 4,
    DPadRight = 8,
    Start = 16,
    Back = 32,
    LeftStick = 64,
    RightStick = 128,
    LeftShoulder = 256,
    RightShoulder = 512,
    BigButton = 2048,
    A = 4096,
    B = 8192,
    X = 16384,
    Y = 32768,
}
