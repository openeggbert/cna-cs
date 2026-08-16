using CNA.Interop;

namespace CNA.Input;

public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex)
    {
        Native.cna_gamepad_get_state((int)playerIndex, out CnaGamePadState state);
        return new GamePadState(state);
    }

    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
    {
        Native.cna_gamepad_get_capabilities((int)playerIndex, out CnaGamePadCapabilities capabilities);
        return new GamePadCapabilities(capabilities);
    }
}
