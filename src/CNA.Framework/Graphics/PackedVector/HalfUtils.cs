namespace CNA.Graphics.PackedVector;

/// <summary>
/// Reproduces XNA 4.0's historical 16-bit floating-point conversion, including its
/// finite exponent-31 representation and saturation of overflow and NaN values.
/// </summary>
internal static class HalfUtils
{
    private const uint MaxNormal = 1_207_955_455u;
    private const uint MinNormal = 947_912_704u;

    public static ushort Pack(float value)
    {
        uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        uint sign = (bits & 0x80000000u) >> 16;
        uint magnitude = bits & 0x7FFFFFFFu;

        if (magnitude > MaxNormal)
        {
            return (ushort)(sign | 0x7FFFu);
        }

        if (magnitude < MinNormal)
        {
            uint mantissa = (magnitude & 0x7FFFFFu) | 0x800000u;
            int shift = 113 - (int)(magnitude >> 23);
            magnitude = shift <= 31 ? mantissa >> shift : 0u;
            return (ushort)(sign | ((magnitude + 4095u + ((magnitude >> 13) & 1u)) >> 13));
        }

        return (ushort)(sign |
            ((magnitude - 939_524_096u + 4095u + ((magnitude >> 13) & 1u)) >> 13));
    }

    public static float Unpack(ushort value)
    {
        uint bits;
        if ((value & 0x7C00) == 0)
        {
            uint mantissa = (uint)value & 0x03FFu;
            if (mantissa == 0)
            {
                bits = (uint)(value & 0x8000) << 16;
            }
            else
            {
                int exponent = -14;
                while ((mantissa & 0x0400u) == 0)
                {
                    exponent--;
                    mantissa <<= 1;
                }

                mantissa &= ~0x0400u;
                bits = ((uint)(value & 0x8000) << 16)
                    | ((uint)(exponent + 127) << 23)
                    | (mantissa << 13);
            }
        }
        else
        {
            bits = ((uint)(value & 0x8000) << 16)
                | ((uint)(((value >> 10) & 0x1F) - 15 + 127) << 23)
                | ((uint)(value & 0x03FF) << 13);
        }

        return BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }
}
