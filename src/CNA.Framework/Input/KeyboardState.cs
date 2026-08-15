using CNA.Interop;

namespace CNA.Framework.Input;

/// <summary>
/// A full-keyboard snapshot taken by <see cref="Keyboard.GetState"/>. One native call per
/// snapshot, then every <see cref="IsKeyDown"/> check is local -- see the "input as snapshots"
/// guidance in ../../cnabinding/analysis_binding.md §25.
/// </summary>
public readonly struct KeyboardState
{
    private readonly CnaKeyboardState _native;

    internal KeyboardState(CnaKeyboardState native)
    {
        _native = native;
    }

    public bool IsKeyDown(Keys key) => _native.IsKeyDown((int)key);

    public bool IsKeyUp(Keys key) => !IsKeyDown(key);
}
