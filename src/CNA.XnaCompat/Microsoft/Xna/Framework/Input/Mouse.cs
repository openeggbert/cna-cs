namespace Microsoft.Xna.Framework.Input;

public static class Mouse
{
    public static MouseState GetState() => new(CNA.Framework.Input.Mouse.GetState());
}
