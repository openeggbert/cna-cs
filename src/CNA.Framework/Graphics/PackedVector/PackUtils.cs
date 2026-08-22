namespace CNA.Graphics.PackedVector;

/// <summary>
/// Implements the quantization rules used by XNA 4.0 packed-vector types.
/// </summary>
internal static class PackUtils
{
    public static uint PackUnsigned(float bitmask, float value)
        => (uint)ClampAndRound(value, 0f, bitmask);

    public static uint PackSigned(uint bitmask, float value)
    {
        float max = bitmask >> 1;
        float min = -max - 1f;
        return (uint)(int)ClampAndRound(value, min, max) & bitmask;
    }

    public static uint PackUNorm(float bitmask, float value)
        => (uint)ClampAndRound(value * bitmask, 0f, bitmask);

    public static float UnpackUNorm(uint bitmask, uint value)
        => (value & bitmask) / (float)bitmask;

    public static uint PackSNorm(uint bitmask, float value)
    {
        float max = bitmask >> 1;
        return (uint)(int)ClampAndRound(value * max, -max, max) & bitmask;
    }

    public static float UnpackSNorm(uint bitmask, uint value)
    {
        uint signBit = bitmask + 1 >> 1;
        if ((value & signBit) != 0)
        {
            if ((value & bitmask) == signBit)
            {
                return -1f;
            }

            value |= ~bitmask;
        }
        else
        {
            value &= bitmask;
        }

        float max = bitmask >> 1;
        return (int)value / max;
    }

    private static double ClampAndRound(float value, float min, float max)
    {
        if (float.IsNaN(value))
        {
            return 0d;
        }

        if (float.IsInfinity(value))
        {
            return float.IsNegativeInfinity(value) ? min : max;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return Math.Round(value);
    }
}
