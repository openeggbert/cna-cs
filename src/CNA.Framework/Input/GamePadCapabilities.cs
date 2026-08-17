using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// What a connected game pad actually supports, from <see cref="GamePad.GetCapabilities"/>. Now
/// matches the real, shipped openeggbert/cna C API's own <c>CNA_GamePadCapabilities</c>
/// (<c>input_gamepad.h</c>) exactly, rather than a self-designed guess -- see <c>NEXT.md</c>'s
/// native-ABI-migration entry, step 11. The real struct has one individual field per capability
/// (confirmed, before writing this constructor, to already match this type's own pre-migration
/// property names and count exactly -- all 24 <c>Has*</c> properties below), not the two packed
/// bitmasks this project's own <c>CnaGamePadCapabilities</c> used to reuse from
/// <see cref="Buttons"/>/an invented <c>Features</c> mask -- so this constructor is now a direct
/// per-field copy instead of bitmask decoding.
/// </summary>
public readonly struct GamePadCapabilities
{
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

        HasAButton = native.HasAButton != 0;
        HasBButton = native.HasBButton != 0;
        HasXButton = native.HasXButton != 0;
        HasYButton = native.HasYButton != 0;
        HasBackButton = native.HasBackButton != 0;
        HasStartButton = native.HasStartButton != 0;
        HasBigButton = native.HasBigButton != 0;
        HasDPadUpButton = native.HasDPadUpButton != 0;
        HasDPadDownButton = native.HasDPadDownButton != 0;
        HasDPadLeftButton = native.HasDPadLeftButton != 0;
        HasDPadRightButton = native.HasDPadRightButton != 0;
        HasLeftShoulderButton = native.HasLeftShoulderButton != 0;
        HasRightShoulderButton = native.HasRightShoulderButton != 0;
        HasLeftStickButton = native.HasLeftStickButton != 0;
        HasRightStickButton = native.HasRightStickButton != 0;

        HasLeftXThumbStick = native.HasLeftXThumbStick != 0;
        HasLeftYThumbStick = native.HasLeftYThumbStick != 0;
        HasRightXThumbStick = native.HasRightXThumbStick != 0;
        HasRightYThumbStick = native.HasRightYThumbStick != 0;
        HasLeftTrigger = native.HasLeftTrigger != 0;
        HasRightTrigger = native.HasRightTrigger != 0;
        HasLeftVibrationMotor = native.HasLeftVibrationMotor != 0;
        HasRightVibrationMotor = native.HasRightVibrationMotor != 0;
        HasVoiceSupport = native.HasVoiceSupport != 0;
    }
}
