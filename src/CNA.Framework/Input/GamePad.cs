using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// Real ABI now needs a game handle (<see cref="CnaAmbientGame.Current"/>) -- no parameterless
/// game pad route exists (<c>input.h</c>/<c>input_gamepad.h</c>), matching every other static
/// input/media subsystem this migration reached. Also now checks each native call's own
/// <see cref="CnaResult"/> -- an ABI-independent bug this step fixes regardless of the ABI
/// mismatch. <see cref="GetState(PlayerIndex)"/> uses <c>cna_gamepad_get_state</c>, matching real XNA's own
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

    /// <summary>Matches real XNA's <c>GetState(PlayerIndex, GamePadDeadZone)</c>: the same snapshot,
    /// with an explicit dead-zone rule applied to the analog axes. Was missing until a sweep of
    /// unbound header functions found it -- the parameterless overload was the only one
    /// bound.</summary>
    public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
    {
        var state = new CnaGamePadState();
        CnaResult result = Native.cna_gamepad_get_state_with_dead_zone(
            CnaAmbientGame.Current, (uint)playerIndex, (uint)deadZoneMode, ref state);
        CnaException.ThrowIfFailed(result, nameof(GetState));
        return new GamePadState(state);
    }

    /// <summary>
    /// Matches real XNA's <c>SetVibration</c>: drives the two rumble motors, each in [0, 1].
    ///
    /// Returns <see langword="false"/> when the controller did not accept it -- a pad with no
    /// motors, or none connected in that slot. That is an ordinary answer, not an error, and is
    /// what real XNA's own <see cref="bool"/> return means.
    ///
    /// Core XNA API that was simply absent. Found by sweeping which header functions are *not*
    /// bound, which is the mirror of the sweep that found the fabricated ones -- the type-level
    /// coverage audit could not see it, because <c>GamePad</c> was present and only a member was
    /// missing.
    /// </summary>
    public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
    {
        CnaResult result = Native.cna_gamepad_set_vibration(
            CnaAmbientGame.Current, (uint)playerIndex, leftMotor, rightMotor, out byte applied);
        CnaException.ThrowIfFailed(result, nameof(SetVibration));
        return applied != 0;
    }

    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
    {
        var capabilities = new CnaGamePadCapabilities();
        CnaResult result = Native.cna_gamepad_get_capabilities(CnaAmbientGame.Current, (uint)playerIndex, ref capabilities);
        CnaException.ThrowIfFailed(result, nameof(GetCapabilities));
        return new GamePadCapabilities(capabilities);
    }
}
