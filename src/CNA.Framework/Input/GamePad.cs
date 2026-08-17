using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// Real ABI now needs a game handle (<see cref="CnaAmbientGame.Current"/>) -- no parameterless
/// game pad route exists (<c>input.h</c>/<c>input_gamepad.h</c>), matching every other static
/// input/media subsystem this migration reached. Also now checks each native call's own
/// <see cref="CnaResult"/> -- an ABI-independent bug this step fixes regardless of the ABI
/// mismatch. <see cref="GetState"/> uses <c>cna_gamepad_get_state</c>, matching real XNA's own
/// default (canonical <c>IndependentAxes</c> dead-zone processing) -- the real ABI also has an
/// explicit-dead-zone-mode overload (<c>cna_gamepad_get_state_with_dead_zone</c>) this project
/// doesn't expose, since nothing in its own public API surface asks for one yet. See
/// <c>NEXT.md</c>'s native-ABI-migration entry, step 11.
/// </summary>
public static class GamePad
{
    public static GamePadState GetState(PlayerIndex playerIndex)
    {
        var state = new CnaGamePadState();
        CnaResult result = Native.cna_gamepad_get_state(CnaAmbientGame.Current, (uint)playerIndex, ref state);
        CnaException.ThrowIfFailed(result, nameof(GetState));
        return new GamePadState(state);
    }

    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
    {
        var capabilities = new CnaGamePadCapabilities();
        CnaResult result = Native.cna_gamepad_get_capabilities(CnaAmbientGame.Current, (uint)playerIndex, ref capabilities);
        CnaException.ThrowIfFailed(result, nameof(GetCapabilities));
        return new GamePadCapabilities(capabilities);
    }
}
