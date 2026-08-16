namespace Microsoft.Xna.Framework.Input;

public readonly struct MouseState
{
    private readonly CNA.Input.MouseState _framework;

    internal MouseState(CNA.Input.MouseState framework)
    {
        _framework = framework;
    }

    public int X => _framework.X;
    public int Y => _framework.Y;
    public Point Position => new(X, Y);
    public int ScrollWheelValue => _framework.ScrollWheelValue;
    public int HorizontalScrollWheelValue => _framework.HorizontalScrollWheelValue;

    public ButtonState LeftButton => ToCompat(_framework.LeftButton);
    public ButtonState MiddleButton => ToCompat(_framework.MiddleButton);
    public ButtonState RightButton => ToCompat(_framework.RightButton);
    public ButtonState XButton1 => ToCompat(_framework.XButton1);
    public ButtonState XButton2 => ToCompat(_framework.XButton2);

    private static ButtonState ToCompat(CNA.Input.ButtonState value) => (ButtonState)(int)value;
}
