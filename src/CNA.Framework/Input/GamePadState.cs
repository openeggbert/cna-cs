using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// A game pad snapshot taken by <see cref="GamePad.GetState(PlayerIndex)"/>. <c>PacketNumber</c>
/// (real XNA increments this only when state actually changes, to let callers skip redundant
/// processing) is not implemented and always reads 0 -- see plan.md Phase 4.
/// </summary>
public readonly struct GamePadState
{
    private readonly CNA.Input.Buttons _rawButtons;

    public bool IsConnected { get; }
    public GamePadButtons Buttons { get; }
    public GamePadDPad DPad { get; }
    public GamePadThumbSticks ThumbSticks { get; }
    public GamePadTriggers Triggers { get; }
    public int PacketNumber => 0;

    internal GamePadState(CnaGamePadState native)
    {
        IsConnected = native.IsConnected != 0;
        _rawButtons = (CNA.Input.Buttons)native.Buttons;
        Buttons = new GamePadButtons(_rawButtons);
        DPad = new GamePadDPad(_rawButtons);
        ThumbSticks = new GamePadThumbSticks(
            new Vector2(native.LeftThumbStickX, native.LeftThumbStickY),
            new Vector2(native.RightThumbStickX, native.RightThumbStickY));
        Triggers = new GamePadTriggers(native.LeftTrigger, native.RightTrigger);
    }

    public bool IsButtonDown(CNA.Input.Buttons button) => _rawButtons.HasFlag(button);

    public bool IsButtonUp(CNA.Input.Buttons button) => !IsButtonDown(button);
}
