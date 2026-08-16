using CNA.Interop;

namespace CNA.Input;

public static class Keyboard
{
    public static KeyboardState GetState()
    {
        Native.cna_keyboard_get_state(out CnaKeyboardState state);
        return new KeyboardState(state);
    }
}
