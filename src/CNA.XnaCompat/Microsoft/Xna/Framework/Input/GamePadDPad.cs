namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadDPad
{
    public ButtonState Up { get; }
    public ButtonState Down { get; }
    public ButtonState Left { get; }
    public ButtonState Right { get; }

    internal GamePadDPad(CNA.Framework.Input.GamePadDPad framework)
    {
        Up = ToCompat(framework.Up);
        Down = ToCompat(framework.Down);
        Left = ToCompat(framework.Left);
        Right = ToCompat(framework.Right);
    }

    private static ButtonState ToCompat(CNA.Framework.Input.ButtonState value) => (ButtonState)(int)value;
}
