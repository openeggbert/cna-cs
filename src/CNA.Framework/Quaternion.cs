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

    /// <summary>
    /// Standard "largest diagonal term" (Shepperd's method) matrix-to-quaternion extraction --
    /// branches on which diagonal term is largest to avoid dividing by a near-zero value, the
    /// textbook fix for the naive trace-based formula's numerical instability near 180-degree
    /// rotations. Assumes <paramref name="matrix"/> is a pure rotation (no scale/shear) in this
    /// project's row-vector convention; round-trips with <see cref="Matrix.CreateFromQuaternion"/>
    /// are checked in QuaternionTests.
    /// </summary>
    public static Quaternion CreateFromRotationMatrix(Matrix matrix)
    {
        float trace = matrix.M11 + matrix.M22 + matrix.M33;

        if (trace > 0f)
        {
            float s = MathF.Sqrt(trace + 1f) * 2f;
            return new Quaternion(
                (matrix.M23 - matrix.M32) / s,
                (matrix.M31 - matrix.M13) / s,
                (matrix.M12 - matrix.M21) / s,
                0.25f * s);
        }

        if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33)
        {
            float s = MathF.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33) * 2f;
            return new Quaternion(
                0.25f * s,
                (matrix.M21 + matrix.M12) / s,
                (matrix.M31 + matrix.M13) / s,
                (matrix.M23 - matrix.M32) / s);
        }

        if (matrix.M22 > matrix.M33)
        {
            float s = MathF.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33) * 2f;
            return new Quaternion(
                (matrix.M21 + matrix.M12) / s,
                0.25f * s,
                (matrix.M32 + matrix.M23) / s,
                (matrix.M31 - matrix.M13) / s);
        }

        {
            float s = MathF.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22) * 2f;
            return new Quaternion(
                (matrix.M31 + matrix.M13) / s,
                (matrix.M32 + matrix.M23) / s,
                0.25f * s,
                (matrix.M12 - matrix.M21) / s);
        }
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

    /// <summary>Spherical linear interpolation, shortest-path-corrected (negates
    /// <paramref name="b"/> when the quaternions are more than 90 degrees apart, matching real
    /// XNA) and falling back to linear interpolation when the two are nearly parallel, where the
    /// great-circle formula becomes numerically unstable.</summary>
    public static Quaternion Slerp(Quaternion a, Quaternion b, float amount)
    {
        float cosOmega = Dot(a, b);
        bool flip = cosOmega < 0f;
        if (flip)
        {
            cosOmega = -cosOmega;
        }

        float weightA, weightB;
        if (cosOmega > 0.999999f)
        {
            weightA = 1f - amount;
            weightB = flip ? -amount : amount;
        }
        else
        {
            float omega = MathF.Acos(cosOmega);
            float inverseSinOmega = 1f / MathF.Sin(omega);
            weightA = MathF.Sin((1f - amount) * omega) * inverseSinOmega;
            weightB = flip
                ? -MathF.Sin(amount * omega) * inverseSinOmega
                : MathF.Sin(amount * omega) * inverseSinOmega;
        }

        return new Quaternion(
            (weightA * a.X) + (weightB * b.X),
            (weightA * a.Y) + (weightB * b.Y),
            (weightA * a.Z) + (weightB * b.Z),
            (weightA * a.W) + (weightB * b.W));
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
