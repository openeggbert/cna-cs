using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// What a connected game pad actually supports, from <see cref="GamePad.GetCapabilities"/>. No
/// ABI shape for this exists anywhere upstream (unlike <see cref="GamePadState"/>'s own ABI,
/// which at least follows the established snapshot-struct pattern) -- self-designed for this
/// repository. <c>Has*Button</c> properties read <see cref="CnaGamePadCapabilities.SupportedButtons"/>
/// using the exact same bit layout as <see cref="CNA.Input.Buttons"/> (so this only supports the
/// same core button subset <see cref="GamePadState"/> does -- no thumbstick-direction-as-button or
/// trigger-as-button capability bits, matching that type's own documented omission).
/// </summary>
public readonly struct GamePadCapabilities
{
    // Bit assignments for CnaGamePadCapabilities.Features -- an internal packing convention
    // invented for this repository's ABI, not derived from any doc.
    private const uint HasLeftXThumbStickBit = 1 << 0;
    private const uint HasLeftYThumbStickBit = 1 << 1;
    private const uint HasRightXThumbStickBit = 1 << 2;
    private const uint HasRightYThumbStickBit = 1 << 3;
    private const uint HasLeftTriggerBit = 1 << 4;
    private const uint HasRightTriggerBit = 1 << 5;
    private const uint HasLeftVibrationMotorBit = 1 << 6;
    private const uint HasRightVibrationMotorBit = 1 << 7;
    private const uint HasVoiceSupportBit = 1 << 8;

    public bool IsConnected { get; }
    public GamePadType GamePadType { get; }

    public bool HasAButton { get; }
    public bool HasBButton { get; }
    public bool HasXButton { get; }
    public bool HasYButton { get; }
    public bool HasBackButton { get; }
    public bool HasStartButton { get; }
    public bool HasBigButton { get; }
    public bool HasDPadUpButton { get; }
    public bool HasDPadDownButton { get; }
    public bool HasDPadLeftButton { get; }
    public bool HasDPadRightButton { get; }
    public bool HasLeftShoulderButton { get; }
    public bool HasRightShoulderButton { get; }
    public bool HasLeftStickButton { get; }
    public bool HasRightStickButton { get; }

    public bool HasLeftXThumbStick { get; }
    public bool HasLeftYThumbStick { get; }
    public bool HasRightXThumbStick { get; }
    public bool HasRightYThumbStick { get; }
    public bool HasLeftTrigger { get; }
    public bool HasRightTrigger { get; }
    public bool HasLeftVibrationMotor { get; }
    public bool HasRightVibrationMotor { get; }
    public bool HasVoiceSupport { get; }

    internal GamePadCapabilities(CnaGamePadCapabilities native)
    {
        IsConnected = native.IsConnected != 0;
        GamePadType = (GamePadType)native.GamePadType;

        var buttons = (Buttons)native.SupportedButtons;
        HasAButton = buttons.HasFlag(Buttons.A);
        HasBButton = buttons.HasFlag(Buttons.B);
        HasXButton = buttons.HasFlag(Buttons.X);
        HasYButton = buttons.HasFlag(Buttons.Y);
        HasBackButton = buttons.HasFlag(Buttons.Back);
        HasStartButton = buttons.HasFlag(Buttons.Start);
        HasBigButton = buttons.HasFlag(Buttons.BigButton);
        HasDPadUpButton = buttons.HasFlag(Buttons.DPadUp);
        HasDPadDownButton = buttons.HasFlag(Buttons.DPadDown);
        HasDPadLeftButton = buttons.HasFlag(Buttons.DPadLeft);
        HasDPadRightButton = buttons.HasFlag(Buttons.DPadRight);
        HasLeftShoulderButton = buttons.HasFlag(Buttons.LeftShoulder);
        HasRightShoulderButton = buttons.HasFlag(Buttons.RightShoulder);
        HasLeftStickButton = buttons.HasFlag(Buttons.LeftStick);
        HasRightStickButton = buttons.HasFlag(Buttons.RightStick);

        uint features = native.Features;
        HasLeftXThumbStick = (features & HasLeftXThumbStickBit) != 0;
        HasLeftYThumbStick = (features & HasLeftYThumbStickBit) != 0;
        HasRightXThumbStick = (features & HasRightXThumbStickBit) != 0;
        HasRightYThumbStick = (features & HasRightYThumbStickBit) != 0;
        HasLeftTrigger = (features & HasLeftTriggerBit) != 0;
        HasRightTrigger = (features & HasRightTriggerBit) != 0;
        HasLeftVibrationMotor = (features & HasLeftVibrationMotorBit) != 0;
        HasRightVibrationMotor = (features & HasRightVibrationMotorBit) != 0;
        HasVoiceSupport = (features & HasVoiceSupportBit) != 0;
    }
}
