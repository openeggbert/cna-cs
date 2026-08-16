namespace Microsoft.Xna.Framework;

public static class MathHelper
{
    public const float E = CNA.MathHelper.E;
    public const float Log10E = CNA.MathHelper.Log10E;
    public const float Log2E = CNA.MathHelper.Log2E;
    public const float Pi = CNA.MathHelper.Pi;
    public const float PiOver2 = CNA.MathHelper.PiOver2;
    public const float PiOver4 = CNA.MathHelper.PiOver4;
    public const float TwoPi = CNA.MathHelper.TwoPi;

    public static float Clamp(float value, float min, float max) => CNA.MathHelper.Clamp(value, min, max);

    public static int Clamp(int value, int min, int max) => CNA.MathHelper.Clamp(value, min, max);

    public static float Distance(float a, float b) => CNA.MathHelper.Distance(a, b);

    public static float Lerp(float a, float b, float amount) => CNA.MathHelper.Lerp(a, b, amount);

    public static float Max(float a, float b) => CNA.MathHelper.Max(a, b);

    public static float Min(float a, float b) => CNA.MathHelper.Min(a, b);

    public static float ToDegrees(float radians) => CNA.MathHelper.ToDegrees(radians);

    public static float ToRadians(float degrees) => CNA.MathHelper.ToRadians(degrees);

    public static float SmoothStep(float a, float b, float amount) => CNA.MathHelper.SmoothStep(a, b, amount);

    public static float WrapAngle(float angle) => CNA.MathHelper.WrapAngle(angle);

    public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2) =>
        CNA.MathHelper.Barycentric(value1, value2, value3, amount1, amount2);

    public static float CatmullRom(float value1, float value2, float value3, float value4, float amount) =>
        CNA.MathHelper.CatmullRom(value1, value2, value3, value4, amount);

    public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount) =>
        CNA.MathHelper.Hermite(value1, tangent1, value2, tangent2, amount);
}
