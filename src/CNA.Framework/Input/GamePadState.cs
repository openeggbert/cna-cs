using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// A game pad snapshot taken by <see cref="GamePad.GetState(PlayerIndex)"/>. <c>PacketNumber</c>
/// is now a real native value (<c>CNA_GamePadState.packet_number</c>) -- previously always read 0
/// (self-designed, no ABI to source it from) before this migration reached <c>input.h</c>. Thumb
/// stick/trigger values are read from <see cref="CnaGamePadState.Analog"/>, the real ABI's own
/// nested sub-struct -- the old guessed shape had them as flat top-level fields on
/// <see cref="CnaGamePadState"/> itself.
/// </summary>
public readonly struct GamePadState
{
    private readonly CNA.Input.Buttons _rawButtons;

    public bool IsConnected { get; }
    public GamePadButtons Buttons { get; }
    public GamePadDPad DPad { get; }
    public GamePadThumbSticks ThumbSticks { get; }
    public GamePadTriggers Triggers { get; }
    public int PacketNumber { get; }

    internal GamePadState(CnaGamePadState native)
    {
        IsConnected = native.IsConnected != 0;
        _rawButtons = (CNA.Input.Buttons)native.Buttons;
        Buttons = new GamePadButtons(_rawButtons);
        DPad = new GamePadDPad(_rawButtons);
        ThumbSticks = new GamePadThumbSticks(
            Vector2.FromNative(native.Analog.LeftThumbStick),
            Vector2.FromNative(native.Analog.RightThumbStick));
        Triggers = new GamePadTriggers(native.Analog.LeftTrigger, native.Analog.RightTrigger);
        PacketNumber = native.PacketNumber;
    }

    public bool IsButtonDown(CNA.Input.Buttons button) => _rawButtons.HasFlag(button);

    public bool IsButtonUp(CNA.Input.Buttons button) => !IsButtonDown(button);
}
