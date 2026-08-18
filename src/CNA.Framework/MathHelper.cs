namespace CNA;

/// <summary>XNA-compatible math constants/helpers.</summary>
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

    public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2) =>
        value1 + (amount1 * (value2 - value1)) + (amount2 * (value3 - value1));

    /// <summary>Standard cubic Catmull-Rom spline basis, evaluated in <c>double</c> and rounded
    /// back to <c>float</c> at the end -- matches real XNA's own precision choice for this
    /// formula, which is otherwise pure textbook spline math, not something specific to XNA.</summary>
    public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
    {
        double amountSquared = (double)amount * amount;
        double amountCubed = amountSquared * amount;

        return (float)(0.5 * (
            (2.0 * value2) +
            ((value3 - value1) * amount) +
            (((2.0 * value1) - (5.0 * value2) + (4.0 * value3) - value4) * amountSquared) +
            (((3.0 * value2) - value1 - (3.0 * value3) + value4) * amountCubed)));
    }

    /// <summary>Standard cubic Hermite spline basis (two endpoint values, two endpoint tangents).
    /// Evaluated in <c>double</c>, matching real XNA's own precision choice; the endpoint
    /// short-circuits (<paramref name="amount"/> exactly 0 or 1) match real XNA's own behavior of
    /// returning the endpoint value exactly rather than whatever the polynomial rounds to.</summary>
    public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
    {
        if (amount == 0f)
        {
            return value1;
        }

        if (amount == 1f)
        {
            return value2;
        }

        double s = amount;
        double sSquared = s * s;
        double sCubed = sSquared * s;
        double v1 = value1, v2 = value2, t1 = tangent1, t2 = tangent2;

        return (float)(
            (((2.0 * v1) - (2.0 * v2) + t2 + t1) * sCubed) +
            (((-3.0 * v1) + (3.0 * v2) - (2.0 * t1) - t2) * sSquared) +
            (t1 * s) +
            v1);
    }

    /// <summary>
    /// The largest <see cref="float"/> that still compares equal to zero when added to one --
    /// 2^-24, about 5.96e-8.
    ///
    /// Deliberately <b>not</b> <see cref="float.Epsilon"/>, which in .NET is the smallest
    /// *denormal* (1.4e-45) and is roughly 37 orders of magnitude too small to be useful as a
    /// comparison tolerance. Computed by the same halving loop the engine's own
    /// <c>MathHelper::GetMachineEpsilonFloat</c> uses, rather than written as a literal, so the two
    /// cannot drift and so the definition is visible rather than asserted.
    /// </summary>
    internal static readonly float MachineEpsilonFloat = ComputeMachineEpsilonFloat();

    /// <summary>Matches the engine's own <c>WithinEpsilon</c>. Internal rather than public: real
    /// XNA's <c>MathHelper</c> has no such member, and this repository's mandate is the XNA 4.0
    /// surface, not a superset of it.</summary>
    internal static bool WithinEpsilon(float a, float b) => MathF.Abs(a - b) < MachineEpsilonFloat;

    private static float ComputeMachineEpsilonFloat()
    {
        float epsilon = 1f;
        float comparison;

        do
        {
            epsilon *= 0.5f;
            comparison = 1f + epsilon;
        }
        while (comparison > 1f);

        return epsilon;
    }
}
