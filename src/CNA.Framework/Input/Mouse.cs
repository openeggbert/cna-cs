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
    /// <summary>
    /// Matches real XNA's <c>SetPosition</c>: warps the cursor to a point inside the bound window.
    ///
    /// Core XNA API -- the whole of mouse-look input depends on it -- and it was absent. Found by
    /// sweeping which header functions are *not* bound.
    /// </summary>
    public static void SetPosition(int x, int y)
    {
        CnaResult result = Native.cna_mouse_set_position(CnaAmbientGame.Current, x, y);
        CnaException.ThrowIfFailed(result, nameof(SetPosition));
    }

    /// <summary>Matches real XNA's <c>WindowHandle</c>: the native window mouse input is bound to.
    /// Zero when none is bound. Typed as <see cref="nint"/> here, matching XNA's own
    /// <c>IntPtr</c>, over an ABI that carries it as a <see cref="ulong"/>.</summary>
    public static nint WindowHandle
    {
        get
        {
            CnaResult result = Native.cna_mouse_get_window_handle(CnaAmbientGame.Current, out ulong window);
            CnaException.ThrowIfFailed(result, nameof(WindowHandle));
            return unchecked((nint)window);
        }
        set
        {
            CnaResult result = Native.cna_mouse_set_window_handle(CnaAmbientGame.Current, unchecked((ulong)value));
            CnaException.ThrowIfFailed(result, nameof(WindowHandle));
        }
    }

    /// <summary>
    /// Sets the cursor image.
    ///
    /// Not XNA: XNA's <c>Mouse</c> says nothing about the cursor's appearance. MonoGame added
    /// <c>Mouse.SetCursor</c> and games ported from it call it, so CNA's cursor surface is offered
    /// here on the CNA layer and re-exported from <c>CNA.XnaCompat.Extensions</c> -- never from the
    /// strict facade, where a member XNA does not have is a contract violation.
    /// </summary>
    public static void SetCursor(MouseCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        CnaResult result = Native.cna_mouse_set_cursor_ext(
            CnaAmbientGame.Current, new CnaHandle(cursor.NativeHandleValue));
        GC.KeepAlive(cursor);
        CnaException.ThrowIfFailed(result, nameof(SetCursor));
    }

    public static MouseState GetState()
    {
        var state = new CnaMouseState();
        CnaResult result = Native.cna_mouse_get_state(CnaAmbientGame.Current, ref state);
        CnaException.ThrowIfFailed(result, nameof(GetState));
        return new MouseState(state);
    }
}
