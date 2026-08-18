namespace Microsoft.Xna.Framework.Input;

/// <summary>XNA 4.0-compatible <c>KeyState</c>. A duplicated enum rather than an alias -- enums
/// cannot define conversion operators, so every crossing is an explicit cast, the same shape
/// <see cref="ButtonState"/> already has.</summary>
public enum KeyState
{
    Up = 0,
    Down = 1,
}
