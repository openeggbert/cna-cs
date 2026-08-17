using CNA.Interop;

namespace CNA.Input;

/// <summary>
/// Real ABI now needs a game handle (<see cref="CnaAmbientGame.Current"/>) -- no parameterless
/// mouse route exists (<c>input.h</c>), matching every other static input/media subsystem this
/// migration reached. Also now checks the native call's own <see cref="CnaResult"/> -- an
/// ABI-independent bug this step fixes regardless of the ABI mismatch. See <c>NEXT.md</c>'s
/// native-ABI-migration entry, step 11.
/// </summary>
public static class Mouse
{
    public static MouseState GetState()
    {
        var state = new CnaMouseState();
        CnaResult result = Native.cna_mouse_get_state(CnaAmbientGame.Current, ref state);
        CnaException.ThrowIfFailed(result, nameof(GetState));
        return new MouseState(state);
    }
}
