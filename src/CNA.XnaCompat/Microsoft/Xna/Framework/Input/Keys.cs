namespace Microsoft.Xna.Framework.Input;

/// <summary>
/// XNA 4.0-compatible <c>Keys</c>. Enums cannot inherit, so this is a separate enum from
/// <see cref="CNA.Framework.Input.Keys"/>, kept numerically identical to it (both match real XNA's
/// Windows virtual-key-code values) so casting between them is always a no-op value cast -- see
/// <see cref="KeysExtensions.ToFrameworkKeys"/>. Same starter subset as
/// <see cref="CNA.Framework.Input.Keys"/>; broaden both together (Phase 4, plan.md).
/// </summary>
public enum Keys
{
    None = 0,
    Back = 8,
    Tab = 9,
    Enter = 13,
    Escape = 27,
    Space = 32,
    PageUp = 33,
    PageDown = 34,
    End = 35,
    Home = 36,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,
    Delete = 46,
    D0 = 48,
    D1 = 49,
    D2 = 50,
    D3 = 51,
    D4 = 52,
    D5 = 53,
    D6 = 54,
    D7 = 55,
    D8 = 56,
    D9 = 57,
    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,
    LeftShift = 160,
    RightShift = 161,
    LeftControl = 162,
    RightControl = 163,
    LeftAlt = 164,
    RightAlt = 165,
}

internal static class KeysExtensions
{
    public static CNA.Framework.Input.Keys ToFrameworkKeys(this Keys key) => (CNA.Framework.Input.Keys)(int)key;
}
