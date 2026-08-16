namespace Microsoft.Xna.Framework;

public static class MathHelper
{
    public const float E = CNA.Framework.MathHelper.E;
    public const float Log10E = CNA.Framework.MathHelper.Log10E;
    public const float Log2E = CNA.Framework.MathHelper.Log2E;
    public const float Pi = CNA.Framework.MathHelper.Pi;
    public const float PiOver2 = CNA.Framework.MathHelper.PiOver2;
    public const float PiOver4 = CNA.Framework.MathHelper.PiOver4;
    public const float TwoPi = CNA.Framework.MathHelper.TwoPi;

    public static float Clamp(float value, float min, float max) => CNA.Framework.MathHelper.Clamp(value, min, max);

    public static int Clamp(int value, int min, int max) => CNA.Framework.MathHelper.Clamp(value, min, max);

    public static float Distance(float a, float b) => CNA.Framework.MathHelper.Distance(a, b);

    public static float Lerp(float a, float b, float amount) => CNA.Framework.MathHelper.Lerp(a, b, amount);

    public static float Max(float a, float b) => CNA.Framework.MathHelper.Max(a, b);

    public static float Min(float a, float b) => CNA.Framework.MathHelper.Min(a, b);

    public static float ToDegrees(float radians) => CNA.Framework.MathHelper.ToDegrees(radians);

    public static float ToRadians(float degrees) => CNA.Framework.MathHelper.ToRadians(degrees);

    public static float SmoothStep(float a, float b, float amount) => CNA.Framework.MathHelper.SmoothStep(a, b, amount);

    public static float WrapAngle(float angle) => CNA.Framework.MathHelper.WrapAngle(angle);
}
