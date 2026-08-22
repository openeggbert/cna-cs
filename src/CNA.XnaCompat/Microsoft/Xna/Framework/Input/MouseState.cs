namespace Microsoft.Xna.Framework.Input;

public readonly struct MouseState
{
    public MouseState(
        int x,
        int y,
        int scrollWheel,
        ButtonState leftButton,
        ButtonState middleButton,
        ButtonState rightButton,
        ButtonState xButton1,
        ButtonState xButton2)
    {
        X = x;
        Y = y;
        ScrollWheelValue = scrollWheel;
        LeftButton = leftButton;
        MiddleButton = middleButton;
        RightButton = rightButton;
        XButton1 = xButton1;
        XButton2 = xButton2;
    }

    internal MouseState(CNA.Input.MouseState framework)
        : this(
            framework.X,
            framework.Y,
            framework.ScrollWheelValue,
            ToCompat(framework.LeftButton),
            ToCompat(framework.MiddleButton),
            ToCompat(framework.RightButton),
            ToCompat(framework.XButton1),
            ToCompat(framework.XButton2))
    {
    }

    public int X { get; }
    public int Y { get; }
    public int ScrollWheelValue { get; }
    public ButtonState LeftButton { get; }
    public ButtonState MiddleButton { get; }
    public ButtonState RightButton { get; }
    public ButtonState XButton1 { get; }
    public ButtonState XButton2 { get; }

    public override bool Equals(object? obj) => obj is MouseState other &&
        X == other.X && Y == other.Y && ScrollWheelValue == other.ScrollWheelValue &&
        LeftButton == other.LeftButton && MiddleButton == other.MiddleButton &&
        RightButton == other.RightButton && XButton1 == other.XButton1 && XButton2 == other.XButton2;

    public override int GetHashCode() =>
        X ^ Y ^ (int)LeftButton ^ (int)RightButton ^ (int)MiddleButton ^
        (int)XButton1 ^ (int)XButton2 ^ ScrollWheelValue;

    public override string ToString()
    {
        var pressed = new List<string>(5);
        AddPressed(pressed, LeftButton, "Left");
        AddPressed(pressed, RightButton, "Right");
        AddPressed(pressed, MiddleButton, "Middle");
        AddPressed(pressed, XButton1, "XButton1");
        AddPressed(pressed, XButton2, "XButton2");
        string buttons = pressed.Count == 0 ? "None" : string.Join(" ", pressed);
        return $"{{X:{X} Y:{Y} Buttons:{buttons} Wheel:{ScrollWheelValue}}}";
    }

    public static bool operator ==(MouseState left, MouseState right) => left.Equals(right);

    public static bool operator !=(MouseState left, MouseState right) => !left.Equals(right);

    private static ButtonState ToCompat(CNA.Input.ButtonState value) => (ButtonState)(int)value;

    private static void AddPressed(ICollection<string> names, ButtonState state, string name)
    {
        if (state == ButtonState.Pressed)
        {
            names.Add(name);
        }
    }
}
