using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Color</c>. See docs/architecture.md for why this duplicates
/// <see cref="CNA.Color"/> rather than subclassing it (structs cannot inherit). Named
/// colors here match real XNA's actual byte values (139 colors + Transparent).
/// </summary>
[System.ComponentModel.TypeConverter(typeof(Design.ColorConverter))]
public struct Color : IPackedVector<uint>, IEquatable<Color>
{
    private uint _packedValue;

    public Color(int r, int g, int b)
        : this(r, g, b, 255)
    {
    }

    public Color(int r, int g, int b, int a)
    {
        _packedValue = PackBytes(ClampToByte(r), ClampToByte(g), ClampToByte(b), ClampToByte(a));
    }

    public Color(float r, float g, float b)
        : this(r, g, b, 1f)
    {
    }

    public Color(float r, float g, float b, float a)
    {
        _packedValue = PackFloats(r, g, b, a);
    }

    public Color(Vector3 vector)
        : this(vector.X, vector.Y, vector.Z, 1f)
    {
    }

    public Color(Vector4 vector)
        : this(vector.X, vector.Y, vector.Z, vector.W)
    {
    }

    private Color(uint packedValue)
    {
        _packedValue = packedValue;
    }

    public byte R
    {
        readonly get => (byte)_packedValue;
        set => _packedValue = (_packedValue & 0xFFFFFF00u) | value;
    }

    public byte G
    {
        readonly get => (byte)(_packedValue >> 8);
        set => _packedValue = (_packedValue & 0xFFFF00FFu) | ((uint)value << 8);
    }

    public byte B
    {
        readonly get => (byte)(_packedValue >> 16);
        set => _packedValue = (_packedValue & 0xFF00FFFFu) | ((uint)value << 16);
    }

    public byte A
    {
        readonly get => (byte)(_packedValue >> 24);
        set => _packedValue = (_packedValue & 0x00FFFFFFu) | ((uint)value << 24);
    }

#pragma warning disable CS3021 // XNA carries this member attribute even without an assembly-level CLS declaration.
    [CLSCompliant(false)]
    public uint PackedValue
    {
        readonly get => _packedValue;
        set => _packedValue = value;
    }
#pragma warning restore CS3021

    void IPackedVector.PackFromVector4(Vector4 vector) => _packedValue = PackFloats(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector3 ToVector3() => new(R / 255f, G / 255f, B / 255f);

    public readonly Vector4 ToVector4() => new(R / 255f, G / 255f, B / 255f, A / 255f);

    public static Color FromNonPremultiplied(Vector4 vector) => new(
        vector.X * vector.W,
        vector.Y * vector.W,
        vector.Z * vector.W,
        vector.W);

    public static Color FromNonPremultiplied(int r, int g, int b, int a) => new(
        ClampToByte((long)r * a / 255),
        ClampToByte((long)g * a / 255),
        ClampToByte((long)b * a / 255),
        ClampToByte(a));

    public static Color Lerp(Color value1, Color value2, float amount)
    {
        int fraction = (int)PackUNorm(65536f, amount);
        int r = value1.R + (((value2.R - value1.R) * fraction) >> 16);
        int g = value1.G + (((value2.G - value1.G) * fraction) >> 16);
        int b = value1.B + (((value2.B - value1.B) * fraction) >> 16);
        int a = value1.A + (((value2.A - value1.A) * fraction) >> 16);
        return new Color(PackBytes(r, g, b, a));
    }

    public static Color Multiply(Color value, float scale)
    {
        float scaled = scale * 65536f;
        uint fixedScale = float.IsNaN(scaled) || scaled <= 0f
            ? 0u
            : scaled >= 16777215f
                ? 16777215u
                : (uint)scaled;

        int r = Math.Min(255, (int)((value.R * fixedScale) >> 16));
        int g = Math.Min(255, (int)((value.G * fixedScale) >> 16));
        int b = Math.Min(255, (int)((value.B * fixedScale) >> 16));
        int a = Math.Min(255, (int)((value.A * fixedScale) >> 16));
        return new Color(PackBytes(r, g, b, a));
    }

    public static Color operator *(Color value, float scale) => Multiply(value, scale);

    private static uint PackFloats(float r, float g, float b, float a) =>
        PackUNorm(255f, r) |
        (PackUNorm(255f, g) << 8) |
        (PackUNorm(255f, b) << 16) |
        (PackUNorm(255f, a) << 24);

    private static uint PackUNorm(float bitmask, float value)
    {
        float scaled = value * bitmask;
        if (float.IsNaN(scaled))
        {
            return 0;
        }

        if (scaled <= 0f)
        {
            return 0;
        }

        if (scaled >= bitmask)
        {
            return (uint)bitmask;
        }

        return (uint)Math.Round((double)scaled);
    }

    private static int ClampToByte(int value) => Math.Clamp(value, 0, 255);

    private static int ClampToByte(long value) => value < 0 ? 0 : value > 255 ? 255 : (int)value;

    private static uint PackBytes(int r, int g, int b, int a) =>
        (uint)(r | (g << 8) | (b << 16) | (a << 24));

    /// <summary>Matches real XNA: white with zero alpha, not black with zero alpha.</summary>
    public static Color Transparent => new(255, 255, 255, 0);

    public static Color AliceBlue => new(240, 248, 255);
    public static Color AntiqueWhite => new(250, 235, 215);
    public static Color Aqua => new(0, 255, 255);
    public static Color Aquamarine => new(127, 255, 212);
    public static Color Azure => new(240, 255, 255);
    public static Color Beige => new(245, 245, 220);
    public static Color Bisque => new(255, 228, 196);
    public static Color Black => new(0, 0, 0);
    public static Color BlanchedAlmond => new(255, 235, 205);
    public static Color Blue => new(0, 0, 255);
    public static Color BlueViolet => new(138, 43, 226);
    public static Color Brown => new(165, 42, 42);
    public static Color BurlyWood => new(222, 184, 135);
    public static Color CadetBlue => new(95, 158, 160);
    public static Color Chartreuse => new(127, 255, 0);
    public static Color Chocolate => new(210, 105, 30);
    public static Color Coral => new(255, 127, 80);
    public static Color CornflowerBlue => new(100, 149, 237);
    public static Color Cornsilk => new(255, 248, 220);
    public static Color Crimson => new(220, 20, 60);
    public static Color Cyan => new(0, 255, 255);
    public static Color DarkBlue => new(0, 0, 139);
    public static Color DarkCyan => new(0, 139, 139);
    public static Color DarkGoldenrod => new(184, 134, 11);
    public static Color DarkGray => new(169, 169, 169);
    public static Color DarkGreen => new(0, 100, 0);
    public static Color DarkKhaki => new(189, 183, 107);
    public static Color DarkMagenta => new(139, 0, 139);
    public static Color DarkOliveGreen => new(85, 107, 47);
    public static Color DarkOrange => new(255, 140, 0);
    public static Color DarkOrchid => new(153, 50, 204);
    public static Color DarkRed => new(139, 0, 0);
    public static Color DarkSalmon => new(233, 150, 122);
    public static Color DarkSeaGreen => new(143, 188, 143);
    public static Color DarkSlateBlue => new(72, 61, 139);
    public static Color DarkSlateGray => new(47, 79, 79);
    public static Color DarkTurquoise => new(0, 206, 209);
    public static Color DarkViolet => new(148, 0, 211);
    public static Color DeepPink => new(255, 20, 147);
    public static Color DeepSkyBlue => new(0, 191, 255);
    public static Color DimGray => new(105, 105, 105);
    public static Color DodgerBlue => new(30, 144, 255);
    public static Color Firebrick => new(178, 34, 34);
    public static Color FloralWhite => new(255, 250, 240);
    public static Color ForestGreen => new(34, 139, 34);
    public static Color Fuchsia => new(255, 0, 255);
    public static Color Gainsboro => new(220, 220, 220);
    public static Color GhostWhite => new(248, 248, 255);
    public static Color Gold => new(255, 215, 0);
    public static Color Goldenrod => new(218, 165, 32);
    public static Color Gray => new(128, 128, 128);
    public static Color Green => new(0, 128, 0);
    public static Color GreenYellow => new(173, 255, 47);
    public static Color Honeydew => new(240, 255, 240);
    public static Color HotPink => new(255, 105, 180);
    public static Color IndianRed => new(205, 92, 92);
    public static Color Indigo => new(75, 0, 130);
    public static Color Ivory => new(255, 255, 240);
    public static Color Khaki => new(240, 230, 140);
    public static Color Lavender => new(230, 230, 250);
    public static Color LavenderBlush => new(255, 240, 245);
    public static Color LawnGreen => new(124, 252, 0);
    public static Color LemonChiffon => new(255, 250, 205);
    public static Color LightBlue => new(173, 216, 230);
    public static Color LightCoral => new(240, 128, 128);
    public static Color LightCyan => new(224, 255, 255);
    public static Color LightGoldenrodYellow => new(250, 250, 210);
    public static Color LightGray => new(211, 211, 211);
    public static Color LightGreen => new(144, 238, 144);
    public static Color LightPink => new(255, 182, 193);
    public static Color LightSalmon => new(255, 160, 122);
    public static Color LightSeaGreen => new(32, 178, 170);
    public static Color LightSkyBlue => new(135, 206, 250);
    public static Color LightSlateGray => new(119, 136, 153);
    public static Color LightSteelBlue => new(176, 196, 222);
    public static Color LightYellow => new(255, 255, 224);
    public static Color Lime => new(0, 255, 0);
    public static Color LimeGreen => new(50, 205, 50);
    public static Color Linen => new(250, 240, 230);
    public static Color Magenta => new(255, 0, 255);
    public static Color Maroon => new(128, 0, 0);
    public static Color MediumAquamarine => new(102, 205, 170);
    public static Color MediumBlue => new(0, 0, 205);
    public static Color MediumOrchid => new(186, 85, 211);
    public static Color MediumPurple => new(147, 112, 219);
    public static Color MediumSeaGreen => new(60, 179, 113);
    public static Color MediumSlateBlue => new(123, 104, 238);
    public static Color MediumSpringGreen => new(0, 250, 154);
    public static Color MediumTurquoise => new(72, 209, 204);
    public static Color MediumVioletRed => new(199, 21, 133);
    public static Color MidnightBlue => new(25, 25, 112);
    public static Color MintCream => new(245, 255, 250);
    public static Color MistyRose => new(255, 228, 225);
    public static Color Moccasin => new(255, 228, 181);
    public static Color NavajoWhite => new(255, 222, 173);
    public static Color Navy => new(0, 0, 128);
    public static Color OldLace => new(253, 245, 230);
    public static Color Olive => new(128, 128, 0);
    public static Color OliveDrab => new(107, 142, 35);
    public static Color Orange => new(255, 165, 0);
    public static Color OrangeRed => new(255, 69, 0);
    public static Color Orchid => new(218, 112, 214);
    public static Color PaleGoldenrod => new(238, 232, 170);
    public static Color PaleGreen => new(152, 251, 152);
    public static Color PaleTurquoise => new(175, 238, 238);
    public static Color PaleVioletRed => new(219, 112, 147);
    public static Color PapayaWhip => new(255, 239, 213);
    public static Color PeachPuff => new(255, 218, 185);
    public static Color Peru => new(205, 133, 63);
    public static Color Pink => new(255, 192, 203);
    public static Color Plum => new(221, 160, 221);
    public static Color PowderBlue => new(176, 224, 230);
    public static Color Purple => new(128, 0, 128);
    public static Color Red => new(255, 0, 0);
    public static Color RosyBrown => new(188, 143, 143);
    public static Color RoyalBlue => new(65, 105, 225);
    public static Color SaddleBrown => new(139, 69, 19);
    public static Color Salmon => new(250, 128, 114);
    public static Color SandyBrown => new(244, 164, 96);
    public static Color SeaGreen => new(46, 139, 87);
    public static Color SeaShell => new(255, 245, 238);
    public static Color Sienna => new(160, 82, 45);
    public static Color Silver => new(192, 192, 192);
    public static Color SkyBlue => new(135, 206, 235);
    public static Color SlateBlue => new(106, 90, 205);
    public static Color SlateGray => new(112, 128, 144);
    public static Color Snow => new(255, 250, 250);
    public static Color SpringGreen => new(0, 255, 127);
    public static Color SteelBlue => new(70, 130, 180);
    public static Color Tan => new(210, 180, 140);
    public static Color Teal => new(0, 128, 128);
    public static Color Thistle => new(216, 191, 216);
    public static Color Tomato => new(255, 99, 71);
    public static Color Turquoise => new(64, 224, 208);
    public static Color Violet => new(238, 130, 238);
    public static Color Wheat => new(245, 222, 179);
    public static Color White => new(255, 255, 255);
    public static Color WhiteSmoke => new(245, 245, 245);
    public static Color Yellow => new(255, 255, 0);
    public static Color YellowGreen => new(154, 205, 50);

    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public readonly bool Equals(Color other) => _packedValue == other._packedValue;
    public override readonly bool Equals(object? obj) => obj is Color other && Equals(other);
    public override readonly int GetHashCode() => _packedValue.GetHashCode();
    public override readonly string ToString() => $"{{R:{R} G:{G} B:{B} A:{A}}}";

    internal readonly CNA.Color ToFramework() => new(R, G, B, A);

    internal static Color FromFramework(CNA.Color value) => new(value.R, value.G, value.B, value.A);
}
