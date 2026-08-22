namespace Microsoft.Xna.Framework.Input;

/// <summary>XNA 4.0-compatible immutable keyboard snapshot.</summary>
public readonly struct KeyboardState
{
    private static readonly uint[] ValidKeyMasks = BuildValidKeyMasks();

    private readonly uint _state0;
    private readonly uint _state1;
    private readonly uint _state2;
    private readonly uint _state3;
    private readonly uint _state4;
    private readonly uint _state5;
    private readonly uint _state6;
    private readonly uint _state7;

    public KeyboardState(params Keys[] keys)
    {
        uint[] packed = PackKeys(keys);
        _state0 = packed[0];
        _state1 = packed[1];
        _state2 = packed[2];
        _state3 = packed[3];
        _state4 = packed[4];
        _state5 = packed[5];
        _state6 = packed[6];
        _state7 = packed[7];
    }

    internal KeyboardState(CNA.Input.KeyboardState framework)
        : this(ConvertPressedKeys(framework.GetPressedKeys()))
    {
    }

    public bool IsKeyDown(Keys key)
    {
        int value = (int)key;
        int word = value >> 5;
        if ((uint)word >= 8u)
        {
            return false;
        }

        uint bit = 1u << (value & 31);
        return (GetWord(word) & bit) != 0;
    }

    public bool IsKeyUp(Keys key) => !IsKeyDown(key);

    public KeyState this[Keys key] => IsKeyDown(key) ? KeyState.Down : KeyState.Up;

    public Keys[] GetPressedKeys()
    {
        var result = new List<Keys>();
        for (int value = 0; value < 256; value++)
        {
            if (IsKeyDown((Keys)value))
            {
                result.Add((Keys)value);
            }
        }

        return [.. result];
    }

    public override bool Equals(object? obj)
    {
        if (obj is not KeyboardState other)
        {
            return false;
        }

        return this == other;
    }

    public override int GetHashCode() => unchecked((int)(
        _state0 ^ _state1 ^ _state2 ^ _state3 ^ _state4 ^ _state5 ^ _state6 ^ _state7));

    public static bool operator ==(KeyboardState a, KeyboardState b) =>
        a._state0 == b._state0 && a._state1 == b._state1 &&
        a._state2 == b._state2 && a._state3 == b._state3 &&
        a._state4 == b._state4 && a._state5 == b._state5 &&
        a._state6 == b._state6 && a._state7 == b._state7;

    public static bool operator !=(KeyboardState a, KeyboardState b) => !(a == b);

    private uint GetWord(int word) => word switch
    {
        0 => _state0,
        1 => _state1,
        2 => _state2,
        3 => _state3,
        4 => _state4,
        5 => _state5,
        6 => _state6,
        7 => _state7,
        _ => 0,
    };

    private static uint[] PackKeys(Keys[]? keys)
    {
        var packed = new uint[8];
        if (keys is null)
        {
            return packed;
        }

        foreach (Keys key in keys)
        {
            int value = (int)key;
            int word = value >> 5;
            if ((uint)word >= 8u)
            {
                continue;
            }

            packed[word] |= (1u << (value & 31)) & ValidKeyMasks[word];
        }

        return packed;
    }

    private static uint[] BuildValidKeyMasks()
    {
        var masks = new uint[8];
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            int value = (int)key;
            int word = value >> 5;
            if ((uint)word < 8u)
            {
                masks[word] |= 1u << (value & 31);
            }
        }

        return masks;
    }

    private static Keys[] ConvertPressedKeys(CNA.Input.Keys[] pressed)
    {
        var result = new Keys[pressed.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = pressed[i].ToCompatKeys();
        }

        return result;
    }
}
