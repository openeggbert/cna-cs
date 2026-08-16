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
}
