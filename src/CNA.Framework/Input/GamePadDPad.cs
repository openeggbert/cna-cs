namespace CNA.Input;

public readonly struct GamePadDPad
{
    public ButtonState Up { get; }
    public ButtonState Down { get; }
    public ButtonState Left { get; }
    public ButtonState Right { get; }

    internal GamePadDPad(Buttons buttons)
    {
        Up = buttons.HasFlag(Buttons.DPadUp) ? ButtonState.Pressed : ButtonState.Released;
        Down = buttons.HasFlag(Buttons.DPadDown) ? ButtonState.Pressed : ButtonState.Released;
        Left = buttons.HasFlag(Buttons.DPadLeft) ? ButtonState.Pressed : ButtonState.Released;
        Right = buttons.HasFlag(Buttons.DPadRight) ? ButtonState.Pressed : ButtonState.Released;
    }
}
