using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// Real ABI now needs a game handle (<see cref="CnaAmbientGame.Current"/>) -- no parameterless
/// keyboard route exists (<c>input.h</c>), matching every other static input/media subsystem this
/// migration reached. Also now checks the native call's own <see cref="CnaResult"/> -- an
/// ABI-independent bug this step fixes regardless of the ABI mismatch: this class never checked a
/// result at all before this migration, unlike every other native call site in this codebase. See
/// <c>NEXT.md</c>'s native-ABI-migration entry, step 11.
/// </summary>
public static class Keyboard
{
    public static KeyboardState GetState()
    {
        var state = new CnaKeyboardState();
        CnaResult result = Native.cna_keyboard_get_state(CnaAmbientGame.Current, ref state);
        CnaException.ThrowIfFailed(result, nameof(GetState));
        return new KeyboardState(state);
    }
}
