namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Vector2</c>. Structs cannot inherit, so this is a small, deliberately
/// duplicated struct with implicit conversions to/from <see cref="CNA.Vector2"/> rather
/// than a subclass -- see docs/architecture.md ("Why the XNA value types are not literally the
/// same type as the CNA ones"). A future codegen tool
/// (tools/binding-generator/) is the intended long-term fix for this duplication.
/// </summary>
[System.ComponentModel.TypeConverter(typeof(Design.Vector2Converter))]
public struct Vector2 : IEquatable<Vector2>
{
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vector2(float value)
    {
        X = value;
        Y = value;
    }

    public static Vector2 Zero => new(0f, 0f);
    public static Vector2 One => new(1f, 1f);
    public static Vector2 UnitX => new(1f, 0f);
    public static Vector2 UnitY => new(0f, 1f);

    public readonly float Length() => (float)Math.Sqrt((X * X) + (Y * Y));

    public readonly float LengthSquared() => X * X + Y * Y;

    public void Normalize()
    {
        float factor = 1f / (float)Math.Sqrt((X * X) + (Y * Y));
        X *= factor;
        Y *= factor;
    }

    public static Vector2 Normalize(Vector2 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector2 value1, Vector2 value2) => (value1 - value2).Length();

    public static float DistanceSquared(Vector2 value1, Vector2 value2) => (value1 - value2).LengthSquared();

    public static float Dot(Vector2 value1, Vector2 value2) => (value1.X * value2.X) + (value1.Y * value2.Y);

    public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount) =>
        new(value1.X + ((value2.X - value1.X) * amount), value1.Y + ((value2.Y - value1.Y) * amount));

    public static Vector2 Min(Vector2 value1, Vector2 value2) =>
        new(value1.X < value2.X ? value1.X : value2.X,
            value1.Y < value2.Y ? value1.Y : value2.Y);

    public static Vector2 Max(Vector2 value1, Vector2 value2) =>
        new(value1.X > value2.X ? value1.X : value2.X,
            value1.Y > value2.Y ? value1.Y : value2.Y);

    public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max) => new(
        MathHelper.Clamp(value1.X, min.X, max.X),
        MathHelper.Clamp(value1.Y, min.Y, max.Y));

    public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float amount) => new(
        MathHelper.SmoothStep(value1.X, value2.X, amount),
        MathHelper.SmoothStep(value1.Y, value2.Y, amount));

    public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2) => new(
        MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
        MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));

    public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount) => new(
        MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
        MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));

    public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float amount) => new(
        MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount),
        MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount));

    public static Vector2 Add(Vector2 value1, Vector2 value2) => value1 + value2;

    public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = value1 + value2;

    public static void Barycentric(
        ref Vector2 value1,
        ref Vector2 value2,
        ref Vector2 value3,
        float amount1,
        float amount2,
        out Vector2 result) => result = Barycentric(value1, value2, value3, amount1, amount2);

    public static void CatmullRom(
        ref Vector2 value1,
        ref Vector2 value2,
        ref Vector2 value3,
        ref Vector2 value4,
        float amount,
        out Vector2 result) => result = CatmullRom(value1, value2, value3, value4, amount);

    public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result) =>
        result = Clamp(value1, min, max);

    public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result) =>
        result = Distance(value1, value2);

    public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result) =>
        result = DistanceSquared(value1, value2);

    public static Vector2 Divide(Vector2 value1, Vector2 value2) => value1 / value2;

    public static Vector2 Divide(Vector2 value1, float divider) => value1 / divider;

    public static void Divide(ref Vector2 value1, float divider, out Vector2 result) =>
        result = value1 / divider;

    public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = value1 / value2;

    public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result) =>
        result = Dot(value1, value2);

    public static void Hermite(
        ref Vector2 value1,
        ref Vector2 tangent1,
        ref Vector2 value2,
        ref Vector2 tangent2,
        float amount,
        out Vector2 result) => result = Hermite(value1, tangent1, value2, tangent2, amount);

    public static void Lerp(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result) =>
        result = Lerp(value1, value2, amount);

    public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = Max(value1, value2);

    public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = Min(value1, value2);

    public static Vector2 Multiply(Vector2 value1, Vector2 value2) => value1 * value2;

    public static Vector2 Multiply(Vector2 value1, float scaleFactor) => value1 * scaleFactor;

    public static void Multiply(ref Vector2 value1, float scaleFactor, out Vector2 result) =>
        result = value1 * scaleFactor;

    public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = value1 * value2;

    public static Vector2 Negate(Vector2 value) => -value;

    public static void Negate(ref Vector2 value, out Vector2 result) => result = -value;

    public static void Normalize(ref Vector2 value, out Vector2 result) => result = Normalize(value);

    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        float factor = 2f * Dot(vector, normal);
        return new Vector2(vector.X - (factor * normal.X), vector.Y - (factor * normal.Y));
    }

    public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result) =>
        result = Reflect(vector, normal);

    public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result) =>
        result = SmoothStep(value1, value2, amount);

    public static Vector2 Subtract(Vector2 value1, Vector2 value2) => value1 - value2;

    public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result) =>
        result = value1 - value2;

    public static Vector2 Transform(Vector2 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out Vector2 result);
        return result;
    }

    public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
    {
        float x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
        float y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
        result = new Vector2(x, y);
    }

    public static Vector2 Transform(Vector2 value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Vector2 result);
        return result;
    }

    public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector2 result)
    {
        float x2 = rotation.X + rotation.X;
        float y2 = rotation.Y + rotation.Y;
        float z2 = rotation.Z + rotation.Z;
        float wz2 = rotation.W * z2;
        float xx2 = rotation.X * x2;
        float xy2 = rotation.X * y2;
        float yy2 = rotation.Y * y2;
        float zz2 = rotation.Z * z2;
        result = new Vector2(
            (value.X * (1f - yy2 - zz2)) + (value.Y * (xy2 - wz2)),
            (value.X * (xy2 + wz2)) + (value.Y * (1f - xx2 - zz2)));
    }

    public static Vector2 TransformNormal(Vector2 normal, Matrix matrix)
    {
        TransformNormal(ref normal, ref matrix, out Vector2 result);
        return result;
    }

    public static void TransformNormal(ref Vector2 normal, ref Matrix matrix, out Vector2 result)
    {
        float x = (normal.X * matrix.M11) + (normal.Y * matrix.M21);
        float y = (normal.X * matrix.M12) + (normal.Y * matrix.M22);
        result = new Vector2(x, y);
    }

    public static void Transform(
        Vector2[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector2[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref matrix, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }

    public static void Transform(
        Vector2[] sourceArray,
        int sourceIndex,
        ref Quaternion rotation,
        Vector2[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref rotation, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector2[] sourceArray, ref Quaternion rotation, Vector2[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref rotation, destinationArray, 0, sourceArray.Length);
    }

    public static void TransformNormal(
        Vector2[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector2[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            TransformNormal(ref sourceArray[sourceIndex + i], ref matrix, out destinationArray[destinationIndex + i]);
        }
    }

    public static void TransformNormal(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        TransformNormal(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }

    private static void ValidateTransformArrays(
        Vector2[] sourceArray,
        int sourceIndex,
        Vector2[] destinationArray,
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

    public static Vector2 operator +(Vector2 value1, Vector2 value2) =>
        new(value1.X + value2.X, value1.Y + value2.Y);
    public static Vector2 operator -(Vector2 value1, Vector2 value2) =>
        new(value1.X - value2.X, value1.Y - value2.Y);
    public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);
    public static Vector2 operator *(Vector2 value, float scaleFactor) =>
        new(value.X * scaleFactor, value.Y * scaleFactor);
    public static Vector2 operator *(float scaleFactor, Vector2 value) =>
        new(value.X * scaleFactor, value.Y * scaleFactor);
    public static Vector2 operator *(Vector2 value1, Vector2 value2) =>
        new(value1.X * value2.X, value1.Y * value2.Y);
    public static Vector2 operator /(Vector2 value1, float divider)
    {
        float factor = 1f / divider;
        return new Vector2(value1.X * factor, value1.Y * factor);
    }
    public static Vector2 operator /(Vector2 value1, Vector2 value2) =>
        new(value1.X / value2.X, value1.Y / value2.Y);

    public static bool operator ==(Vector2 value1, Vector2 value2) =>
        value1.X == value2.X && value1.Y == value2.Y;
    public static bool operator !=(Vector2 value1, Vector2 value2) => !(value1 == value2);

    public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is Vector2 other && Equals(other);
    public override readonly int GetHashCode() => X.GetHashCode() + Y.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y}}}";

    internal readonly CNA.Vector2 ToFramework() => new(X, Y);

    internal static Vector2 FromFramework(CNA.Vector2 value) => new(value.X, value.Y);
}
