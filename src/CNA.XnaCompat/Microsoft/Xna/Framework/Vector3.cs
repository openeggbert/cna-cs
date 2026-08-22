namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Vector3</c>. Storage (fields) is duplicated like Vector2.cs/Color.cs,
/// but all behavior delegates to <see cref="CNA.Vector3"/> through internal
/// conversion methods rather than re-deriving the formulas a second time -- see
/// docs/architecture.md.
/// </summary>
[System.ComponentModel.TypeConverter(typeof(Design.Vector3Converter))]
public struct Vector3 : IEquatable<Vector3>
{
    public float X;
    public float Y;
    public float Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3(Vector2 value, float z)
    {
        X = value.X;
        Y = value.Y;
        Z = z;
    }

    public Vector3(float value)
    {
        X = Y = Z = value;
    }

    public static Vector3 Zero => new(0f, 0f, 0f);
    public static Vector3 One => new(1f, 1f, 1f);
    public static Vector3 UnitX => new(1f, 0f, 0f);
    public static Vector3 UnitY => new(0f, 1f, 0f);
    public static Vector3 UnitZ => new(0f, 0f, 1f);
    public static Vector3 Up => new(0f, 1f, 0f);
    public static Vector3 Down => new(0f, -1f, 0f);
    public static Vector3 Right => new(1f, 0f, 0f);
    public static Vector3 Left => new(-1f, 0f, 0f);
    public static Vector3 Forward => new(0f, 0f, -1f);
    public static Vector3 Backward => new(0f, 0f, 1f);

    public readonly float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

    public void Normalize()
    {
        float factor = 1f / (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        X *= factor;
        Y *= factor;
        Z *= factor;
    }

    public static Vector3 Normalize(Vector3 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector3 value1, Vector3 value2) => (value1 - value2).Length();

    public static float DistanceSquared(Vector3 value1, Vector3 value2) => (value1 - value2).LengthSquared();

    public static float Dot(Vector3 vector1, Vector3 vector2) =>
        (vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z);

    public static Vector3 Cross(Vector3 vector1, Vector3 vector2) => new(
        (vector1.Y * vector2.Z) - (vector1.Z * vector2.Y),
        (vector1.Z * vector2.X) - (vector1.X * vector2.Z),
        (vector1.X * vector2.Y) - (vector1.Y * vector2.X));

    public static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        float dot =
            (vector.X * normal.X) +
            (vector.Y * normal.Y) +
            (vector.Z * normal.Z);
        return new Vector3(
            vector.X - (2f * dot * normal.X),
            vector.Y - (2f * dot * normal.Y),
            vector.Z - (2f * dot * normal.Z));
    }

    public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount) => new(
        value1.X + ((value2.X - value1.X) * amount),
        value1.Y + ((value2.Y - value1.Y) * amount),
        value1.Z + ((value2.Z - value1.Z) * amount));

    public static Vector3 Min(Vector3 value1, Vector3 value2) => new(
        value1.X < value2.X ? value1.X : value2.X,
        value1.Y < value2.Y ? value1.Y : value2.Y,
        value1.Z < value2.Z ? value1.Z : value2.Z);

    public static Vector3 Max(Vector3 value1, Vector3 value2) => new(
        value1.X > value2.X ? value1.X : value2.X,
        value1.Y > value2.Y ? value1.Y : value2.Y,
        value1.Z > value2.Z ? value1.Z : value2.Z);

    public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max) => new(
        MathHelper.Clamp(value1.X, min.X, max.X),
        MathHelper.Clamp(value1.Y, min.Y, max.Y),
        MathHelper.Clamp(value1.Z, min.Z, max.Z));

    public static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float amount) => new(
        MathHelper.SmoothStep(value1.X, value2.X, amount),
        MathHelper.SmoothStep(value1.Y, value2.Y, amount),
        MathHelper.SmoothStep(value1.Z, value2.Z, amount));

    public static Vector3 Barycentric(Vector3 value1, Vector3 value2, Vector3 value3, float amount1, float amount2) => new(
        MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
        MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
        MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));

    public static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float amount) => new(
        MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
        MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
        MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount));

    public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount) => new(
        MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount),
        MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount),
        MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount));

    public static Vector3 Transform(Vector3 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out Vector3 result);
        return result;
    }

    public static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
    {
        TransformNormal(ref normal, ref matrix, out Vector3 result);
        return result;
    }

    public static Vector3 Transform(Vector3 value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Vector3 result);
        return result;
    }

    public static Vector3 Add(Vector3 value1, Vector3 value2) => value1 + value2;

    public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = value1 + value2;

    public static void Barycentric(
        ref Vector3 value1,
        ref Vector3 value2,
        ref Vector3 value3,
        float amount1,
        float amount2,
        out Vector3 result) => result = Barycentric(value1, value2, value3, amount1, amount2);

    public static void CatmullRom(
        ref Vector3 value1,
        ref Vector3 value2,
        ref Vector3 value3,
        ref Vector3 value4,
        float amount,
        out Vector3 result) => result = CatmullRom(value1, value2, value3, value4, amount);

    public static void Clamp(ref Vector3 value1, ref Vector3 min, ref Vector3 max, out Vector3 result) =>
        result = Clamp(value1, min, max);

    public static void Cross(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result) =>
        result = Cross(vector1, vector2);

    public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result) =>
        result = Distance(value1, value2);

    public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result) =>
        result = DistanceSquared(value1, value2);

    public static Vector3 Divide(Vector3 value1, Vector3 value2) => value1 / value2;

    public static Vector3 Divide(Vector3 value1, float value2) => value1 / value2;

    public static void Divide(ref Vector3 value1, float value2, out Vector3 result) =>
        result = value1 / value2;

    public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = value1 / value2;

    public static void Dot(ref Vector3 vector1, ref Vector3 vector2, out float result) =>
        result = Dot(vector1, vector2);

    public static void Hermite(
        ref Vector3 value1,
        ref Vector3 tangent1,
        ref Vector3 value2,
        ref Vector3 tangent2,
        float amount,
        out Vector3 result) => result = Hermite(value1, tangent1, value2, tangent2, amount);

    public static void Lerp(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result) =>
        result = Lerp(value1, value2, amount);

    public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = Max(value1, value2);

    public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = Min(value1, value2);

    public static Vector3 Multiply(Vector3 value1, Vector3 value2) => value1 * value2;

    public static Vector3 Multiply(Vector3 value1, float scaleFactor) => value1 * scaleFactor;

    public static void Multiply(ref Vector3 value1, float scaleFactor, out Vector3 result) =>
        result = value1 * scaleFactor;

    public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = value1 * value2;

    public static Vector3 Negate(Vector3 value) => -value;

    public static void Negate(ref Vector3 value, out Vector3 result) => result = -value;

    public static void Normalize(ref Vector3 value, out Vector3 result) => result = Normalize(value);

    public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result) =>
        result = Reflect(vector, normal);

    public static void SmoothStep(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result) =>
        result = SmoothStep(value1, value2, amount);

    public static Vector3 Subtract(Vector3 value1, Vector3 value2) => value1 - value2;

    public static void Subtract(ref Vector3 value1, ref Vector3 value2, out Vector3 result) =>
        result = value1 - value2;

    public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector3 result)
    {
        float x = (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41;
        float y = (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42;
        float z = (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43;
        result = new Vector3(x, y, z);
    }

    public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector3 result)
    {
        float x2 = rotation.X + rotation.X;
        float y2 = rotation.Y + rotation.Y;
        float z2 = rotation.Z + rotation.Z;
        float wx2 = rotation.W * x2;
        float wy2 = rotation.W * y2;
        float wz2 = rotation.W * z2;
        float xx2 = rotation.X * x2;
        float xy2 = rotation.X * y2;
        float xz2 = rotation.X * z2;
        float yy2 = rotation.Y * y2;
        float yz2 = rotation.Y * z2;
        float zz2 = rotation.Z * z2;
        result = new Vector3(
            (value.X * (1f - yy2 - zz2)) + (value.Y * (xy2 - wz2)) + (value.Z * (xz2 + wy2)),
            (value.X * (xy2 + wz2)) + (value.Y * (1f - xx2 - zz2)) + (value.Z * (yz2 - wx2)),
            (value.X * (xz2 - wy2)) + (value.Y * (yz2 + wx2)) + (value.Z * (1f - xx2 - yy2)));
    }

    public static void TransformNormal(ref Vector3 normal, ref Matrix matrix, out Vector3 result)
    {
        float x = (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31);
        float y = (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32);
        float z = (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33);
        result = new Vector3(x, y, z);
    }

    public static void Transform(
        Vector3[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector3[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref matrix, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }

    public static void Transform(
        Vector3[] sourceArray,
        int sourceIndex,
        ref Quaternion rotation,
        Vector3[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref rotation, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector3[] sourceArray, ref Quaternion rotation, Vector3[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref rotation, destinationArray, 0, sourceArray.Length);
    }

    public static void TransformNormal(
        Vector3[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector3[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            TransformNormal(ref sourceArray[sourceIndex + i], ref matrix, out destinationArray[destinationIndex + i]);
        }
    }

    public static void TransformNormal(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        TransformNormal(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }

    private static void ValidateTransformArrays(
        Vector3[] sourceArray,
        int sourceIndex,
        Vector3[] destinationArray,
        int destinationIndex,
        int length)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        ArgumentNullException.ThrowIfNull(destinationArray);
        if ((long)sourceArray.Length < (long)sourceIndex + length)
        {
            throw new ArgumentException("The source array is too small.");
        }

        if ((long)destinationArray.Length < (long)destinationIndex + length)
        {
            throw new ArgumentException("The destination array is too small.");
        }
    }

    public static Vector3 operator +(Vector3 value1, Vector3 value2) =>
        new(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
    public static Vector3 operator -(Vector3 value1, Vector3 value2) =>
        new(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);
    public static Vector3 operator *(Vector3 value, float scaleFactor) =>
        new(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);
    public static Vector3 operator *(float scaleFactor, Vector3 value) => value * scaleFactor;
    public static Vector3 operator *(Vector3 value1, Vector3 value2) =>
        new(value1.X * value2.X, value1.Y * value2.Y, value1.Z * value2.Z);
    public static Vector3 operator /(Vector3 value, float divider)
    {
        float factor = 1f / divider;
        return new Vector3(value.X * factor, value.Y * factor, value.Z * factor);
    }
    public static Vector3 operator /(Vector3 value1, Vector3 value2) =>
        new(value1.X / value2.X, value1.Y / value2.Y, value1.Z / value2.Z);

    public static bool operator ==(Vector3 value1, Vector3 value2) =>
        value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
    public static bool operator !=(Vector3 value1, Vector3 value2) => !(value1 == value2);

    public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
    public override readonly bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override readonly int GetHashCode() => X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z}}}";

    internal readonly CNA.Vector3 ToFramework() => new(X, Y, Z);

    internal static Vector3 FromFramework(CNA.Vector3 value) => new(value.X, value.Y, value.Z);
}
