namespace CNA.Framework.Input;

public readonly struct GamePadButtons
{
    public ButtonState A { get; }
    public ButtonState B { get; }
    public ButtonState X { get; }
    public ButtonState Y { get; }
    public ButtonState Back { get; }
    public ButtonState Start { get; }
    public ButtonState BigButton { get; }
    public ButtonState LeftShoulder { get; }
    public ButtonState RightShoulder { get; }
    public ButtonState LeftStick { get; }
    public ButtonState RightStick { get; }

    internal GamePadButtons(Buttons buttons)
    {
        A = ToState(buttons, Buttons.A);
        B = ToState(buttons, Buttons.B);
        X = ToState(buttons, Buttons.X);
        Y = ToState(buttons, Buttons.Y);
        Back = ToState(buttons, Buttons.Back);
        Start = ToState(buttons, Buttons.Start);
        BigButton = ToState(buttons, Buttons.BigButton);
        LeftShoulder = ToState(buttons, Buttons.LeftShoulder);
        RightShoulder = ToState(buttons, Buttons.RightShoulder);
        LeftStick = ToState(buttons, Buttons.LeftStick);
        RightStick = ToState(buttons, Buttons.RightStick);
    }

    private static ButtonState ToState(Buttons value, Buttons flag) =>
        value.HasFlag(flag) ? ButtonState.Pressed : ButtonState.Released;
}
