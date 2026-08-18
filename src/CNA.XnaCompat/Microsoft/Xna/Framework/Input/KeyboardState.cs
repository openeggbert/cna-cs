namespace Microsoft.Xna.Framework.Input;

/// <summary>XNA 4.0-compatible <c>KeyboardState</c>, wrapping a <see cref="CNA.Input.KeyboardState"/> snapshot.</summary>
public readonly struct KeyboardState
{
    private readonly CNA.Input.KeyboardState _framework;

    internal KeyboardState(CNA.Input.KeyboardState framework)
    {
        _framework = framework;
    }

    public bool IsKeyDown(Keys key) => _framework.IsKeyDown(key.ToFrameworkKeys());

    public bool IsKeyUp(Keys key) => !IsKeyDown(key);

    /// <summary>Matches real XNA's indexer. Landed with <see cref="KeyState"/> in the WP16
    /// re-audit; both were missing.</summary>
    public KeyState this[Keys key] => IsKeyDown(key) ? KeyState.Down : KeyState.Up;

    /// <summary>Every key currently down, re-typed into this namespace's own
    /// <see cref="Keys"/>.</summary>
    public Keys[] GetPressedKeys()
    {
        CNA.Input.Keys[] pressed = _framework.GetPressedKeys();
        var result = new Keys[pressed.Length];
        for (int i = 0; i < pressed.Length; i++)
        {
            result[i] = pressed[i].ToCompatKeys();
        }

        return result;
    }
}
