namespace Microsoft.Xna.Framework.Input;

public static class Keyboard
{
    public static KeyboardState GetState() => new(CNA.Input.Keyboard.GetState());
}
