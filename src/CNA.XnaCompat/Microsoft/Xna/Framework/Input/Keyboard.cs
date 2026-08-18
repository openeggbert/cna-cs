namespace Microsoft.Xna.Framework.Input;

public static class Keyboard
{
    public static KeyboardState GetState() => new(CNA.Input.Keyboard.GetState());

    /// <summary>Matches real XNA's <c>GetState(PlayerIndex)</c>.</summary>
    public static KeyboardState GetState(PlayerIndex playerIndex) =>
        new(CNA.Input.Keyboard.GetState((CNA.PlayerIndex)(int)playerIndex));
}
