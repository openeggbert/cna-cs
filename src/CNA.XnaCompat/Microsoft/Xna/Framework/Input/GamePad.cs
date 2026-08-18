namespace Microsoft.Xna.Framework.Input;

public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex) =>
        new(CNA.Input.GamePad.GetState((CNA.PlayerIndex)(int)playerIndex));

    /// <summary>Matches real XNA's <c>GetState(PlayerIndex, GamePadDeadZone)</c>.</summary>
    public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode) =>
        new(CNA.Input.GamePad.GetState(
            (CNA.PlayerIndex)(int)playerIndex, (CNA.Input.GamePadDeadZone)(int)deadZoneMode));

    /// <summary>Matches real XNA's <c>SetVibration</c>. <see langword="false"/> means the controller
    /// did not accept it, not that the call failed -- see
    /// <see cref="CNA.Input.GamePad.SetVibration"/>.</summary>
    public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor) =>
        CNA.Input.GamePad.SetVibration((CNA.PlayerIndex)(int)playerIndex, leftMotor, rightMotor);

    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex) =>
        new(CNA.Input.GamePad.GetCapabilities((CNA.PlayerIndex)(int)playerIndex));
}
