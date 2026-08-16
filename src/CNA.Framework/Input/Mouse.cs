using CNA.Interop;

namespace CNA.Framework.Input;

public static class Mouse
{
    public static MouseState GetState()
    {
        Native.cna_mouse_get_state(out CnaMouseState state);
        return new MouseState(state);
    }
}
