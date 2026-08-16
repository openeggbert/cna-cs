namespace CNA.Framework;

/// <summary>
/// XNA-compatible math constants/helpers. <c>Barycentric</c>, <c>CatmullRom</c>, and
/// <c>Hermite</c> (spline interpolation) are not implemented yet -- see plan.md Phase 4.
/// </summary>
public static class MathHelper
{
    public const float E = 2.71828183f;
    public const float Log10E = 0.4342945f;
    public const float Log2E = 1.442695f;
    public const float Pi = MathF.PI;
    public const float PiOver2 = MathF.PI / 2f;
    public const float PiOver4 = MathF.PI / 4f;
    public const float TwoPi = MathF.PI * 2f;

    public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

    public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);

    public static float Distance(float a, float b) => MathF.Abs(a - b);

    public static float Lerp(float a, float b, float amount) => a + ((b - a) * amount);

    public static float Max(float a, float b) => MathF.Max(a, b);

    public static float Min(float a, float b) => MathF.Min(a, b);

    public static float ToDegrees(float radians) => radians * (180f / Pi);

    public static float ToRadians(float degrees) => degrees * (Pi / 180f);

    public static float SmoothStep(float a, float b, float amount)
    {
        float t = Clamp(amount, 0f, 1f);
        t = (t * t) * (3f - (2f * t));
        return a + ((b - a) * t);
    }

    public static float WrapAngle(float angle)
    {
        angle %= TwoPi;
        if (angle < -Pi)
        {
            angle += TwoPi;
        }
        else if (angle > Pi)
        {
            angle -= TwoPi;
        }

        return angle;
    }
}
