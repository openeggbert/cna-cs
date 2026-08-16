namespace Microsoft.Xna.Framework.Input;

public static class Mouse
{
    public static MouseState GetState() => new(CNA.Input.Mouse.GetState());
}
