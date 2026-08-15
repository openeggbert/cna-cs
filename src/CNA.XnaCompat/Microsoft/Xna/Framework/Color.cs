namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Color</c>. See docs/architecture.md for why this duplicates
/// <see cref="CNA.Framework.Color"/> rather than subclassing it (structs cannot inherit). Named
/// colors here match real XNA's actual byte values. The full ~140-color table and packed-uint
/// layout parity are Phase 4 (plan.md).
/// </summary>
public struct Color : IEquatable<Color>
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public Color(float r, float g, float b, float a = 1f)
        : this(ToByte(r), ToByte(g), ToByte(b), ToByte(a))
    {
    }

    private static byte ToByte(float value) => (byte)Math.Clamp(value * 255f, 0f, 255f);

    public static Color Transparent => new(0, 0, 0, 0);
    public static Color White => new(255, 255, 255);
    public static Color Black => new(0, 0, 0);
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 128, 0);
    public static Color Blue => new(0, 0, 255);
    public static Color Yellow => new(255, 255, 0);
    public static Color Orange => new(255, 165, 0);
    public static Color Purple => new(128, 0, 128);
    public static Color Gray => new(128, 128, 128);
    public static Color LightGray => new(211, 211, 211);
    public static Color DarkGray => new(169, 169, 169);
    public static Color CornflowerBlue => new(100, 149, 237);

    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override readonly bool Equals(object? obj) => obj is Color other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override readonly string ToString() => $"{{R:{R} G:{G} B:{B} A:{A}}}";

    public static implicit operator CNA.Framework.Color(Color value) => new(value.R, value.G, value.B, value.A);
    public static implicit operator Color(CNA.Framework.Color value) => new(value.R, value.G, value.B, value.A);
}
