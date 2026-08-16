namespace Microsoft.Xna.Framework.Input;

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

    internal GamePadButtons(CNA.Framework.Input.GamePadButtons framework)
    {
        A = ToCompat(framework.A);
        B = ToCompat(framework.B);
        X = ToCompat(framework.X);
        Y = ToCompat(framework.Y);
        Back = ToCompat(framework.Back);
        Start = ToCompat(framework.Start);
        BigButton = ToCompat(framework.BigButton);
        LeftShoulder = ToCompat(framework.LeftShoulder);
        RightShoulder = ToCompat(framework.RightShoulder);
        LeftStick = ToCompat(framework.LeftStick);
        RightStick = ToCompat(framework.RightStick);
    }

    private static ButtonState ToCompat(CNA.Framework.Input.ButtonState value) => (ButtonState)(int)value;
}
