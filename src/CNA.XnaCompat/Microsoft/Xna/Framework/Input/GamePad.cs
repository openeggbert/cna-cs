namespace Microsoft.Xna.Framework.Input;

public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex) =>
        new(CNA.Framework.Input.GamePad.GetState((CNA.Framework.Input.PlayerIndex)(int)playerIndex));
}
