namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadDPad
{
    public ButtonState Up { get; }
    public ButtonState Down { get; }
    public ButtonState Left { get; }
    public ButtonState Right { get; }

    public GamePadDPad(
        ButtonState upValue,
        ButtonState downValue,
        ButtonState leftValue,
        ButtonState rightValue)
    {
        Up = upValue;
        Down = downValue;
        Left = leftValue;
        Right = rightValue;
    }

    internal GamePadDPad(CNA.Input.GamePadDPad framework)
    {
        Up = ToCompat(framework.Up);
        Down = ToCompat(framework.Down);
        Left = ToCompat(framework.Left);
        Right = ToCompat(framework.Right);
    }

    private static ButtonState ToCompat(CNA.Input.ButtonState value) => (ButtonState)(int)value;

    public override bool Equals(object? obj) => obj is GamePadDPad other &&
        Up == other.Up && Down == other.Down && Left == other.Left && Right == other.Right;

    public override int GetHashCode() => XnaInputHash.Smart((int)Up, (int)Down, (int)Left, (int)Right);

    public override string ToString()
    {
        var pressed = new List<string>(4);
        AddPressed(pressed, Up, "Up");
        AddPressed(pressed, Down, "Down");
        AddPressed(pressed, Left, "Left");
        AddPressed(pressed, Right, "Right");
        return $"{{DPad:{(pressed.Count == 0 ? "None" : string.Join(" ", pressed))}}}";
    }

    public static bool operator ==(GamePadDPad left, GamePadDPad right) => left.Equals(right);

    public static bool operator !=(GamePadDPad left, GamePadDPad right) => !left.Equals(right);

    private static void AddPressed(ICollection<string> names, ButtonState state, string name)
    {
        if (state == ButtonState.Pressed)
        {
            names.Add(name);
        }
    }
}
