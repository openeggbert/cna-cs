namespace CNA;

/// <summary>Local, managed rotation quaternion -- see the rationale in Vector2.cs.</summary>
public struct Quaternion : IEquatable<Quaternion>
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public Quaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Quaternion(Vector3 vectorPart, float scalarPart)
    {
        X = vectorPart.X;
        Y = vectorPart.Y;
        Z = vectorPart.Z;
        W = scalarPart;
    }

    public static Quaternion Identity => new(0f, 0f, 0f, 1f);

    public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float half = angle * 0.5f;
        float sin = MathF.Sin(half);
        float cos = MathF.Cos(half);
        return new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos);
    }

    public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
    {
        float halfYaw = yaw * 0.5f;
        float halfPitch = pitch * 0.5f;
        float halfRoll = roll * 0.5f;

        float sinYaw = MathF.Sin(halfYaw), cosYaw = MathF.Cos(halfYaw);
        float sinPitch = MathF.Sin(halfPitch), cosPitch = MathF.Cos(halfPitch);
        float sinRoll = MathF.Sin(halfRoll), cosRoll = MathF.Cos(halfRoll);

        return new Quaternion(
            (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll),
            (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll),
            (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll),
            (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll));
    }

    public readonly float Length() => MathF.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));

    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);

    public void Normalize()
    {
        float length = Length();
        if (length >= float.Epsilon)
        {
            X /= length;
            Y /= length;
            Z /= length;
            W /= length;
        }
    }

    public static Quaternion Normalize(Quaternion value)
    {
        value.Normalize();
        return value;
    }

    public static Quaternion Conjugate(Quaternion value) => new(-value.X, -value.Y, -value.Z, value.W);

    public static Quaternion Inverse(Quaternion value)
    {
        float lengthSquared = value.LengthSquared();
        if (lengthSquared < float.Epsilon)
        {
            return Identity;
        }

        float inverseLengthSquared = 1f / lengthSquared;
        Quaternion conjugate = Conjugate(value);
        return new Quaternion(
            conjugate.X * inverseLengthSquared,
            conjugate.Y * inverseLengthSquared,
            conjugate.Z * inverseLengthSquared,
            conjugate.W * inverseLengthSquared);
    }

    public static float Dot(Quaternion a, Quaternion b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);

    public static Quaternion Concatenate(Quaternion value1, Quaternion value2) => value2 * value1;

    public static Quaternion Lerp(Quaternion a, Quaternion b, float amount)
    {
        float inverse = 1f - amount;
        Quaternion result;
        if (Dot(a, b) >= 0f)
        {
            result = new Quaternion(
                (inverse * a.X) + (amount * b.X),
                (inverse * a.Y) + (amount * b.Y),
                (inverse * a.Z) + (amount * b.Z),
                (inverse * a.W) + (amount * b.W));
        }
        else
        {
            result = new Quaternion(
                (inverse * a.X) - (amount * b.X),
                (inverse * a.Y) - (amount * b.Y),
                (inverse * a.Z) - (amount * b.Z),
                (inverse * a.W) - (amount * b.W));
        }

        result.Normalize();
        return result;
    }

    public static Quaternion operator *(Quaternion a, Quaternion b) => new(
        (b.W * a.X) + (b.X * a.W) + (b.Y * a.Z) - (b.Z * a.Y),
        (b.W * a.Y) - (b.X * a.Z) + (b.Y * a.W) + (b.Z * a.X),
        (b.W * a.Z) + (b.X * a.Y) - (b.Y * a.X) + (b.Z * a.W),
        (b.W * a.W) - (b.X * a.X) - (b.Y * a.Y) - (b.Z * a.Z));

    public static Quaternion operator +(Quaternion a, Quaternion b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Quaternion operator -(Quaternion a, Quaternion b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Quaternion operator -(Quaternion value) => new(-value.X, -value.Y, -value.Z, -value.W);

    public static bool operator ==(Quaternion a, Quaternion b) => a.Equals(b);
    public static bool operator !=(Quaternion a, Quaternion b) => !a.Equals(b);

    public readonly bool Equals(Quaternion other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    public override readonly bool Equals(object? obj) => obj is Quaternion other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";
}
