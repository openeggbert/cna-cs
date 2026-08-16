using CNA.Interop;

namespace CNA.Input;

/// <summary><c>GetCapabilities</c> is not implemented -- see plan.md Phase 4.</summary>
public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex)
    {
        Native.cna_gamepad_get_state((int)playerIndex, out CnaGamePadState state);
        return new GamePadState(state);
    }
}
