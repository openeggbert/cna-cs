namespace Microsoft.Xna.Framework.Input;

public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex) =>
        new(CNA.Input.GamePad.GetState((CNA.PlayerIndex)(int)playerIndex));

    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex) =>
        new(CNA.Input.GamePad.GetCapabilities((CNA.PlayerIndex)(int)playerIndex));
}
