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

    public GamePadButtons(Buttons buttons)
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

    internal GamePadButtons(CNA.Input.GamePadButtons framework)
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

    private static ButtonState ToCompat(CNA.Input.ButtonState value) => (ButtonState)(int)value;

    private static ButtonState ToState(Buttons value, Buttons flag) =>
        (value & flag) != 0 ? ButtonState.Pressed : ButtonState.Released;

    public override bool Equals(object? obj) => obj is GamePadButtons other &&
        A == other.A && B == other.B && X == other.X && Y == other.Y &&
        Back == other.Back && Start == other.Start && BigButton == other.BigButton &&
        LeftShoulder == other.LeftShoulder && RightShoulder == other.RightShoulder &&
        LeftStick == other.LeftStick && RightStick == other.RightStick;

    public override int GetHashCode() => XnaInputHash.Smart(
        (int)A, (int)B, (int)X, (int)Y,
        (int)LeftShoulder, (int)RightShoulder, (int)LeftStick, (int)RightStick,
        (int)Start, (int)Back, (int)BigButton);

    public override string ToString()
    {
        var pressed = new List<string>(11);
        AddPressed(pressed, A, "A");
        AddPressed(pressed, B, "B");
        AddPressed(pressed, X, "X");
        AddPressed(pressed, Y, "Y");
        AddPressed(pressed, LeftShoulder, "LeftShoulder");
        AddPressed(pressed, RightShoulder, "RightShoulder");
        AddPressed(pressed, LeftStick, "LeftStick");
        AddPressed(pressed, RightStick, "RightStick");
        AddPressed(pressed, Start, "Start");
        AddPressed(pressed, Back, "Back");
        AddPressed(pressed, BigButton, "BigButton");
        return $"{{Buttons:{(pressed.Count == 0 ? "None" : string.Join(" ", pressed))}}}";
    }

    public static bool operator ==(GamePadButtons left, GamePadButtons right) => left.Equals(right);

    public static bool operator !=(GamePadButtons left, GamePadButtons right) => !left.Equals(right);

    private static void AddPressed(ICollection<string> names, ButtonState state, string name)
    {
        if (state == ButtonState.Pressed)
        {
            names.Add(name);
        }
    }
}
