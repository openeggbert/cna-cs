namespace Microsoft.Xna.Framework.Input;

/// <summary>See CNA.Input.Buttons for which flags are (and are not) implemented; bit
/// values here are kept numerically identical to it.</summary>
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
