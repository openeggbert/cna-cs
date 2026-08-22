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

    public static float Clamp(float value, float min, float max)
    {
        // XNA predates Math.Clamp and deliberately does not validate min <= max.
        value = value > max ? max : value;
        value = value < min ? min : value;
        return value;
    }

    public static float Distance(float value1, float value2) => Math.Abs(value1 - value2);

    public static float Lerp(float value1, float value2, float amount) =>
        value1 + ((value2 - value1) * amount);

    public static float Max(float value1, float value2) => Math.Max(value1, value2);

    public static float Min(float value1, float value2) => Math.Min(value1, value2);

    public static float ToDegrees(float radians) => radians * 57.295776f;

    public static float ToRadians(float degrees) => degrees * 0.017453292f;

    public static float SmoothStep(float value1, float value2, float amount)
    {
        float clampedAmount = Clamp(amount, 0f, 1f);
        return Lerp(
            value1,
            value2,
            (clampedAmount * clampedAmount) * (3f - (2f * clampedAmount)));
    }

    public static float WrapAngle(float angle)
    {
        // XNA uses the double-precision IEEE remainder operation, then rounds to float.
        angle = (float)Math.IEEERemainder(angle, TwoPi);
        if (angle <= -Pi)
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

    public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
    {
        float amountSquared = amount * amount;
        float amountCubed = amount * amountSquared;
        return 0.5f * (
            (2f * value2) +
            ((-value1 + value3) * amount) +
            ((((2f * value1) - (5f * value2)) + (4f * value3) - value4) * amountSquared) +
            ((((-value1 + (3f * value2)) - (3f * value3)) + value4) * amountCubed));
    }

    public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
    {
        float amountSquared = amount * amount;
        float amountCubed = amount * amountSquared;
        float value1Basis = ((2f * amountCubed) - (3f * amountSquared)) + 1f;
        float value2Basis = (-2f * amountCubed) + (3f * amountSquared);
        float tangent1Basis = (amountCubed - (2f * amountSquared)) + amount;
        float tangent2Basis = amountCubed - amountSquared;

        return (value1 * value1Basis) +
            (value2 * value2Basis) +
            (tangent1 * tangent1Basis) +
            (tangent2 * tangent2Basis);
    }
}
