namespace Microsoft.Xna.Framework.Input;

public static class Keyboard
{
    public static KeyboardState GetState() => new(CNA.Framework.Input.Keyboard.GetState());
}
