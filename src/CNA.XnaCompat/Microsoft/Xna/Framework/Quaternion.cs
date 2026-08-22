namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>Quaternion</c>. See Vector3.cs for the duplicate-storage,
/// delegate-behavior pattern used here.</summary>
[System.ComponentModel.TypeConverter(typeof(Design.QuaternionConverter))]
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
        float halfAngle = angle * 0.5f;
        float sin = (float)Math.Sin(halfAngle);
        float cos = (float)Math.Cos(halfAngle);
        return new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos);
    }

    public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
    {
        float halfRoll = roll * 0.5f;
        float sinRoll = (float)Math.Sin(halfRoll);
        float cosRoll = (float)Math.Cos(halfRoll);
        float halfPitch = pitch * 0.5f;
        float sinPitch = (float)Math.Sin(halfPitch);
        float cosPitch = (float)Math.Cos(halfPitch);
        float halfYaw = yaw * 0.5f;
        float sinYaw = (float)Math.Sin(halfYaw);
        float cosYaw = (float)Math.Cos(halfYaw);

        return new Quaternion(
            (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll),
            (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll),
            (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll),
            (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll));
    }

    public static Quaternion CreateFromRotationMatrix(Matrix matrix)
    {
        float trace = matrix.M11 + matrix.M22 + matrix.M33;
        if (trace > 0f)
        {
            float root = (float)Math.Sqrt(trace + 1f);
            float factor = 0.5f / root;
            return new Quaternion(
                (matrix.M23 - matrix.M32) * factor,
                (matrix.M31 - matrix.M13) * factor,
                (matrix.M12 - matrix.M21) * factor,
                root * 0.5f);
        }

        if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
        {
            float root = (float)Math.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33);
            float factor = 0.5f / root;
            return new Quaternion(
                0.5f * root,
                (matrix.M12 + matrix.M21) * factor,
                (matrix.M13 + matrix.M31) * factor,
                (matrix.M23 - matrix.M32) * factor);
        }

        if (matrix.M22 > matrix.M33)
        {
            float root = (float)Math.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33);
            float factor = 0.5f / root;
            return new Quaternion(
                (matrix.M21 + matrix.M12) * factor,
                0.5f * root,
                (matrix.M32 + matrix.M23) * factor,
                (matrix.M31 - matrix.M13) * factor);
        }

        {
            float root = (float)Math.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22);
            float factor = 0.5f / root;
            return new Quaternion(
                (matrix.M31 + matrix.M13) * factor,
                (matrix.M32 + matrix.M23) * factor,
                0.5f * root,
                (matrix.M12 - matrix.M21) * factor);
        }
    }

    public readonly float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));

    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);

    public void Normalize()
    {
        float factor = 1f / (float)Math.Sqrt(LengthSquared());
        X *= factor;
        Y *= factor;
        Z *= factor;
        W *= factor;
    }

    public static Quaternion Normalize(Quaternion quaternion)
    {
        quaternion.Normalize();
        return quaternion;
    }

    public static Quaternion Conjugate(Quaternion value) => new(-value.X, -value.Y, -value.Z, value.W);

    public static Quaternion Inverse(Quaternion quaternion)
    {
        float inverseLengthSquared = 1f / quaternion.LengthSquared();
        return new Quaternion(
            -quaternion.X * inverseLengthSquared,
            -quaternion.Y * inverseLengthSquared,
            -quaternion.Z * inverseLengthSquared,
            quaternion.W * inverseLengthSquared);
    }

    public static float Dot(Quaternion quaternion1, Quaternion quaternion2) =>
        (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) +
        (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);

    public static Quaternion Concatenate(Quaternion value1, Quaternion value2) => value2 * value1;

    public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
    {
        float inverse = 1f - amount;
        Quaternion result = Dot(quaternion1, quaternion2) >= 0f
            ? new Quaternion(
                (inverse * quaternion1.X) + (amount * quaternion2.X),
                (inverse * quaternion1.Y) + (amount * quaternion2.Y),
                (inverse * quaternion1.Z) + (amount * quaternion2.Z),
                (inverse * quaternion1.W) + (amount * quaternion2.W))
            : new Quaternion(
                (inverse * quaternion1.X) - (amount * quaternion2.X),
                (inverse * quaternion1.Y) - (amount * quaternion2.Y),
                (inverse * quaternion1.Z) - (amount * quaternion2.Z),
                (inverse * quaternion1.W) - (amount * quaternion2.W));
        return Normalize(result);
    }

    public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
    {
        float cosOmega = Dot(quaternion1, quaternion2);
        bool flip = false;
        if (cosOmega < 0f)
        {
            flip = true;
            cosOmega = -cosOmega;
        }

        float weight1;
        float weight2;
        if (cosOmega > 0.999999f)
        {
            weight1 = 1f - amount;
            weight2 = flip ? -amount : amount;
        }
        else
        {
            float omega = (float)Math.Acos(cosOmega);
            float inverseSinOmega = (float)(1.0 / Math.Sin(omega));
            weight1 = (float)Math.Sin((1f - amount) * omega) * inverseSinOmega;
            weight2 = flip
                ? (float)(-Math.Sin(amount * omega)) * inverseSinOmega
                : (float)Math.Sin(amount * omega) * inverseSinOmega;
        }

        return new Quaternion(
            (weight1 * quaternion1.X) + (weight2 * quaternion2.X),
            (weight1 * quaternion1.Y) + (weight2 * quaternion2.Y),
            (weight1 * quaternion1.Z) + (weight2 * quaternion2.Z),
            (weight1 * quaternion1.W) + (weight2 * quaternion2.W));
    }

    public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2) => quaternion1 + quaternion2;

    public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result) =>
        result = quaternion1 + quaternion2;

    public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result) =>
        result = Concatenate(value1, value2);

    public void Conjugate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    public static void Conjugate(ref Quaternion value, out Quaternion result) => result = Conjugate(value);

    public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result) =>
        result = CreateFromAxisAngle(axis, angle);

    public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result) =>
        result = CreateFromRotationMatrix(matrix);

    public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result) =>
        result = CreateFromYawPitchRoll(yaw, pitch, roll);

    public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2) => quaternion1 / quaternion2;

    public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result) =>
        result = quaternion1 / quaternion2;

    public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result) =>
        result = Dot(quaternion1, quaternion2);

    public static void Inverse(ref Quaternion quaternion, out Quaternion result) => result = Inverse(quaternion);

    public static void Lerp(
        ref Quaternion quaternion1,
        ref Quaternion quaternion2,
        float amount,
        out Quaternion result) => result = Lerp(quaternion1, quaternion2, amount);

    public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2) => quaternion1 * quaternion2;

    public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor) => quaternion1 * scaleFactor;

    public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result) =>
        result = quaternion1 * scaleFactor;

    public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result) =>
        result = quaternion1 * quaternion2;

    public static Quaternion Negate(Quaternion quaternion) => -quaternion;

    public static void Negate(ref Quaternion quaternion, out Quaternion result) => result = -quaternion;

    public static void Normalize(ref Quaternion quaternion, out Quaternion result) => result = Normalize(quaternion);

    public static void Slerp(
        ref Quaternion quaternion1,
        ref Quaternion quaternion2,
        float amount,
        out Quaternion result) => result = Slerp(quaternion1, quaternion2, amount);

    public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2) => quaternion1 - quaternion2;

    public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result) =>
        result = quaternion1 - quaternion2;

    public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
    {
        float crossX = (quaternion1.Y * quaternion2.Z) - (quaternion1.Z * quaternion2.Y);
        float crossY = (quaternion1.Z * quaternion2.X) - (quaternion1.X * quaternion2.Z);
        float crossZ = (quaternion1.X * quaternion2.Y) - (quaternion1.Y * quaternion2.X);
        float dot =
            (quaternion1.X * quaternion2.X) +
            (quaternion1.Y * quaternion2.Y) +
            (quaternion1.Z * quaternion2.Z);
        return new Quaternion(
            (quaternion1.X * quaternion2.W) + (quaternion2.X * quaternion1.W) + crossX,
            (quaternion1.Y * quaternion2.W) + (quaternion2.Y * quaternion1.W) + crossY,
            (quaternion1.Z * quaternion2.W) + (quaternion2.Z * quaternion1.W) + crossZ,
            (quaternion1.W * quaternion2.W) - dot);
    }
    public static Quaternion operator *(Quaternion quaternion1, float scaleFactor) => new(
        quaternion1.X * scaleFactor,
        quaternion1.Y * scaleFactor,
        quaternion1.Z * scaleFactor,
        quaternion1.W * scaleFactor);
    public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2) =>
        quaternion1 * Inverse(quaternion2);
    public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2) => new(
        quaternion1.X + quaternion2.X,
        quaternion1.Y + quaternion2.Y,
        quaternion1.Z + quaternion2.Z,
        quaternion1.W + quaternion2.W);
    public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2) => new(
        quaternion1.X - quaternion2.X,
        quaternion1.Y - quaternion2.Y,
        quaternion1.Z - quaternion2.Z,
        quaternion1.W - quaternion2.W);
    public static Quaternion operator -(Quaternion quaternion) => new(
        -quaternion.X,
        -quaternion.Y,
        -quaternion.Z,
        -quaternion.W);

    public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2) =>
        quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y &&
        quaternion1.Z == quaternion2.Z && quaternion1.W == quaternion2.W;
    public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2) => !(quaternion1 == quaternion2);

    public readonly bool Equals(Quaternion other) =>
        X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    public override readonly bool Equals(object? obj) => obj is Quaternion other && Equals(other);
    public override readonly int GetHashCode() =>
        X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";

    internal readonly CNA.Quaternion ToFramework() => new(X, Y, Z, W);

    internal static Quaternion FromFramework(CNA.Quaternion value) => new(value.X, value.Y, value.Z, value.W);
}
