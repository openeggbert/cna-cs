namespace Microsoft.Xna.Framework.Input;

/// <summary>XNA 4.0-compatible <c>KeyboardState</c>, wrapping a <see cref="CNA.Framework.Input.KeyboardState"/> snapshot.</summary>
public readonly struct KeyboardState
{
    private readonly CNA.Framework.Input.KeyboardState _framework;

    internal KeyboardState(CNA.Framework.Input.KeyboardState framework)
    {
        _framework = framework;
    }

    public bool IsKeyDown(Keys key) => _framework.IsKeyDown(key.ToFrameworkKeys());

    public bool IsKeyUp(Keys key) => !IsKeyDown(key);
}
