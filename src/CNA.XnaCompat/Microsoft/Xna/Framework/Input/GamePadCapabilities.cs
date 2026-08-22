namespace Microsoft.Xna.Framework.Input;

/// <summary>See CNA.Input.GamePadCapabilities for the ABI/bit-layout caveats (self-designed,
/// no upstream shape). Wraps rather than subclasses, same as this namespace's own
/// <c>GamePadState</c>, since <c>GamePadType</c> needs to be this namespace's own distinct
/// enum type.</summary>
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

    internal GamePadCapabilities(CNA.Input.GamePadCapabilities framework)
    {
        IsConnected = framework.IsConnected;
        GamePadType = framework.GamePadType.ToCompat();

        HasAButton = framework.HasAButton;
        HasBButton = framework.HasBButton;
        HasXButton = framework.HasXButton;
        HasYButton = framework.HasYButton;
        HasBackButton = framework.HasBackButton;
        HasStartButton = framework.HasStartButton;
        HasBigButton = framework.HasBigButton;
        HasDPadUpButton = framework.HasDPadUpButton;
        HasDPadDownButton = framework.HasDPadDownButton;
        HasDPadLeftButton = framework.HasDPadLeftButton;
        HasDPadRightButton = framework.HasDPadRightButton;
        HasLeftShoulderButton = framework.HasLeftShoulderButton;
        HasRightShoulderButton = framework.HasRightShoulderButton;
        HasLeftStickButton = framework.HasLeftStickButton;
        HasRightStickButton = framework.HasRightStickButton;

        HasLeftXThumbStick = framework.HasLeftXThumbStick;
        HasLeftYThumbStick = framework.HasLeftYThumbStick;
        HasRightXThumbStick = framework.HasRightXThumbStick;
        HasRightYThumbStick = framework.HasRightYThumbStick;
        HasLeftTrigger = framework.HasLeftTrigger;
        HasRightTrigger = framework.HasRightTrigger;
        HasLeftVibrationMotor = framework.HasLeftVibrationMotor;
        HasRightVibrationMotor = framework.HasRightVibrationMotor;
        HasVoiceSupport = framework.HasVoiceSupport;
    }
}
