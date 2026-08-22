namespace Microsoft.Xna.Framework.Input;

/// <summary>The exact XNA 4.0 button mask, including its virtual thumbstick-direction and trigger
/// flags. Physical-button values shared with CNA remain numerically identical.</summary>
[Flags]
public enum Buttons
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
    LeftThumbstickLeft = 0x00200000,
    LeftThumbstickRight = 0x40000000,
    LeftThumbstickDown = 0x20000000,
    LeftThumbstickUp = 0x10000000,
    RightThumbstickLeft = 0x08000000,
    RightThumbstickRight = 0x04000000,
    RightThumbstickDown = 0x02000000,
    RightThumbstickUp = 0x01000000,
    LeftTrigger = 0x00800000,
    RightTrigger = 0x00400000,
}
