namespace Microsoft.Xna.Framework.Input;

public static class Mouse
{
    public static MouseState GetState() => new(CNA.Input.Mouse.GetState());

    /// <summary>Matches real XNA's <c>SetPosition</c>. Landed with the CNA-side member -- see
    /// <see cref="CNA.Input.Mouse.SetPosition"/> for how it came to be missing.</summary>
    public static void SetPosition(int x, int y) => CNA.Input.Mouse.SetPosition(x, y);

    /// <summary>Matches real XNA's <c>WindowHandle</c>.</summary>
    public static nint WindowHandle
    {
        get => CNA.Input.Mouse.WindowHandle;
        set => CNA.Input.Mouse.WindowHandle = value;
    }
}
