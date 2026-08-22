namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>Vector4</c>. See Vector3.cs for the duplicate-storage,
/// delegate-behavior pattern used here.</summary>
[System.ComponentModel.TypeConverter(typeof(Design.Vector4Converter))]
public struct Vector4 : IEquatable<Vector4>
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4(Vector2 value, float z, float w)
    {
        X = value.X;
        Y = value.Y;
        Z = z;
        W = w;
    }

    public Vector4(Vector3 value, float w)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
        W = w;
    }

    public Vector4(float value)
    {
        X = Y = Z = W = value;
    }

    public static Vector4 Zero => new(0f, 0f, 0f, 0f);
    public static Vector4 One => new(1f, 1f, 1f, 1f);
    public static Vector4 UnitX => new(1f, 0f, 0f, 0f);
    public static Vector4 UnitY => new(0f, 1f, 0f, 0f);
    public static Vector4 UnitZ => new(0f, 0f, 1f, 0f);
    public static Vector4 UnitW => new(0f, 0f, 0f, 1f);

    public readonly float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));

    public readonly float LengthSquared() => (X * X) + (Y * Y) + (Z * Z) + (W * W);

    public void Normalize()
    {
        float factor = 1f / (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        X *= factor;
        Y *= factor;
        Z *= factor;
        W *= factor;
    }

    public static Vector4 Normalize(Vector4 vector)
    {
        vector.Normalize();
        return vector;
    }

    public static float Distance(Vector4 value1, Vector4 value2) => (value1 - value2).Length();

    public static float DistanceSquared(Vector4 value1, Vector4 value2) => (value1 - value2).LengthSquared();

    public static float Dot(Vector4 vector1, Vector4 vector2) =>
        (vector1.X * vector2.X) + (vector1.Y * vector2.Y) +
        (vector1.Z * vector2.Z) + (vector1.W * vector2.W);

    public static Vector4 Lerp(Vector4 value1, Vector4 value2, float amount) => new(
        value1.X + ((value2.X - value1.X) * amount),
        value1.Y + ((value2.Y - value1.Y) * amount),
        value1.Z + ((value2.Z - value1.Z) * amount),
        value1.W + ((value2.W - value1.W) * amount));

    public static Vector4 Min(Vector4 value1, Vector4 value2) => new(
        value1.X < value2.X ? value1.X : value2.X,
        value1.Y < value2.Y ? value1.Y : value2.Y,
        value1.Z < value2.Z ? value1.Z : value2.Z,
        value1.W < value2.W ? value1.W : value2.W);

    public static Vector4 Max(Vector4 value1, Vector4 value2) => new(
        value1.X > value2.X ? value1.X : value2.X,
        value1.Y > value2.Y ? value1.Y : value2.Y,
        value1.Z > value2.Z ? value1.Z : value2.Z,
        value1.W > value2.W ? value1.W : value2.W);

    public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max) => new(
        MathHelper.Clamp(value1.X, min.X, max.X),
        MathHelper.Clamp(value1.Y, min.Y, max.Y),
        MathHelper.Clamp(value1.Z, min.Z, max.Z),
        MathHelper.Clamp(value1.W, min.W, max.W));

    public static Vector4 SmoothStep(Vector4 value1, Vector4 value2, float amount) => new(
        MathHelper.SmoothStep(value1.X, value2.X, amount),
        MathHelper.SmoothStep(value1.Y, value2.Y, amount),
        MathHelper.SmoothStep(value1.Z, value2.Z, amount),
        MathHelper.SmoothStep(value1.W, value2.W, amount));

    public static Vector4 Barycentric(Vector4 value1, Vector4 value2, Vector4 value3, float amount1, float amount2) => new(
        MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
        MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
        MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2),
        MathHelper.Barycentric(value1.W, value2.W, value3.W, amount1, amount2));

    public static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float amount) => new(
        MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
        MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
        MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount),
        MathHelper.CatmullRom(value1.W, value2.W, value3.W, value4.W, amount));

    public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float amount) => new(
        MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount),
        MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount),
        MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount),
        MathHelper.Hermite(value1.W, tangent1.W, value2.W, tangent2.W, amount));

    public static Vector4 Transform(Vector4 vector, Matrix matrix)
    {
        Transform(ref vector, ref matrix, out Vector4 result);
        return result;
    }

    public static Vector4 Add(Vector4 value1, Vector4 value2) => value1 + value2;

    public static void Add(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = value1 + value2;

    public static void Barycentric(
        ref Vector4 value1,
        ref Vector4 value2,
        ref Vector4 value3,
        float amount1,
        float amount2,
        out Vector4 result) => result = Barycentric(value1, value2, value3, amount1, amount2);

    public static void CatmullRom(
        ref Vector4 value1,
        ref Vector4 value2,
        ref Vector4 value3,
        ref Vector4 value4,
        float amount,
        out Vector4 result) => result = CatmullRom(value1, value2, value3, value4, amount);

    public static void Clamp(ref Vector4 value1, ref Vector4 min, ref Vector4 max, out Vector4 result) =>
        result = Clamp(value1, min, max);

    public static void Distance(ref Vector4 value1, ref Vector4 value2, out float result) =>
        result = Distance(value1, value2);

    public static void DistanceSquared(ref Vector4 value1, ref Vector4 value2, out float result) =>
        result = DistanceSquared(value1, value2);

    public static Vector4 Divide(Vector4 value1, Vector4 value2) => value1 / value2;

    public static Vector4 Divide(Vector4 value1, float divider) => value1 / divider;

    public static void Divide(ref Vector4 value1, float divider, out Vector4 result) =>
        result = value1 / divider;

    public static void Divide(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = value1 / value2;

    public static void Dot(ref Vector4 vector1, ref Vector4 vector2, out float result) =>
        result = Dot(vector1, vector2);

    public static void Hermite(
        ref Vector4 value1,
        ref Vector4 tangent1,
        ref Vector4 value2,
        ref Vector4 tangent2,
        float amount,
        out Vector4 result) => result = Hermite(value1, tangent1, value2, tangent2, amount);

    public static void Lerp(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result) =>
        result = Lerp(value1, value2, amount);

    public static void Max(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = Max(value1, value2);

    public static void Min(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = Min(value1, value2);

    public static Vector4 Multiply(Vector4 value1, Vector4 value2) => value1 * value2;

    public static Vector4 Multiply(Vector4 value1, float scaleFactor) => value1 * scaleFactor;

    public static void Multiply(ref Vector4 value1, float scaleFactor, out Vector4 result) =>
        result = value1 * scaleFactor;

    public static void Multiply(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = value1 * value2;

    public static Vector4 Negate(Vector4 value) => -value;

    public static void Negate(ref Vector4 value, out Vector4 result) => result = -value;

    public static void Normalize(ref Vector4 vector, out Vector4 result) => result = Normalize(vector);

    public static void SmoothStep(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result) =>
        result = SmoothStep(value1, value2, amount);

    public static Vector4 Subtract(Vector4 value1, Vector4 value2) => value1 - value2;

    public static void Subtract(ref Vector4 value1, ref Vector4 value2, out Vector4 result) =>
        result = value1 - value2;

    public static Vector4 Transform(Vector2 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out Vector4 result);
        return result;
    }

    public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector4 result) => result = new(
        (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41,
        (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42,
        (position.X * matrix.M13) + (position.Y * matrix.M23) + matrix.M43,
        (position.X * matrix.M14) + (position.Y * matrix.M24) + matrix.M44);

    public static Vector4 Transform(Vector3 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out Vector4 result);
        return result;
    }

    public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector4 result) => result = new(
        (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
        (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
        (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43,
        (position.X * matrix.M14) + (position.Y * matrix.M24) + (position.Z * matrix.M34) + matrix.M44);

    public static void Transform(ref Vector4 vector, ref Matrix matrix, out Vector4 result)
    {
        float x = (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + (vector.W * matrix.M41);
        float y = (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + (vector.W * matrix.M42);
        float z = (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + (vector.W * matrix.M43);
        float w = (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + (vector.W * matrix.M44);
        result = new Vector4(x, y, z, w);
    }

    public static Vector4 Transform(Vector2 value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Vector4 result);
        return result;
    }

    public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector4 result)
    {
        var value3 = new Vector3(value, 0f);
        Vector3.Transform(ref value3, ref rotation, out Vector3 rotated);
        result = new Vector4(rotated, 1f);
    }

    public static Vector4 Transform(Vector3 value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Vector4 result);
        return result;
    }

    public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector4 result)
    {
        Vector3.Transform(ref value, ref rotation, out Vector3 rotated);
        result = new Vector4(rotated, 1f);
    }

    public static Vector4 Transform(Vector4 value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Vector4 result);
        return result;
    }

    public static void Transform(ref Vector4 value, ref Quaternion rotation, out Vector4 result)
    {
        var value3 = new Vector3(value.X, value.Y, value.Z);
        Vector3.Transform(ref value3, ref rotation, out Vector3 rotated);
        result = new Vector4(rotated, value.W);
    }

    public static void Transform(
        Vector4[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector4[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref matrix, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector4[] sourceArray, ref Matrix matrix, Vector4[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }

    public static void Transform(
        Vector4[] sourceArray,
        int sourceIndex,
        ref Quaternion rotation,
        Vector4[] destinationArray,
        int destinationIndex,
        int length)
    {
        ValidateTransformArrays(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        for (int i = 0; i < length; i++)
        {
            Transform(ref sourceArray[sourceIndex + i], ref rotation, out destinationArray[destinationIndex + i]);
        }
    }

    public static void Transform(Vector4[] sourceArray, ref Quaternion rotation, Vector4[] destinationArray)
    {
        ArgumentNullException.ThrowIfNull(sourceArray);
        Transform(sourceArray, 0, ref rotation, destinationArray, 0, sourceArray.Length);
    }

    private static void ValidateTransformArrays(
        Vector4[] sourceArray,
        int sourceIndex,
        Vector4[] destinationArray,
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

    public static Vector4 operator +(Vector4 value1, Vector4 value2) => new(
        value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z, value1.W + value2.W);
    public static Vector4 operator -(Vector4 value1, Vector4 value2) => new(
        value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z, value1.W - value2.W);
    public static Vector4 operator -(Vector4 value) => new(-value.X, -value.Y, -value.Z, -value.W);
    public static Vector4 operator *(Vector4 value1, float scaleFactor) => new(
        value1.X * scaleFactor, value1.Y * scaleFactor, value1.Z * scaleFactor, value1.W * scaleFactor);
    public static Vector4 operator *(float scaleFactor, Vector4 value1) => value1 * scaleFactor;
    public static Vector4 operator *(Vector4 value1, Vector4 value2) => new(
        value1.X * value2.X, value1.Y * value2.Y, value1.Z * value2.Z, value1.W * value2.W);
    public static Vector4 operator /(Vector4 value1, float divider)
    {
        float factor = 1f / divider;
        return new Vector4(
            value1.X * factor,
            value1.Y * factor,
            value1.Z * factor,
            value1.W * factor);
    }
    public static Vector4 operator /(Vector4 value1, Vector4 value2) => new(
        value1.X / value2.X, value1.Y / value2.Y, value1.Z / value2.Z, value1.W / value2.W);

    public static bool operator ==(Vector4 value1, Vector4 value2) =>
        value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z && value1.W == value2.W;
    public static bool operator !=(Vector4 value1, Vector4 value2) => !(value1 == value2);

    public readonly bool Equals(Vector4 other) =>
        X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    public override readonly bool Equals(object? obj) => obj is Vector4 other && Equals(other);
    public override readonly int GetHashCode() =>
        X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";

    internal readonly CNA.Vector4 ToFramework() => new(X, Y, Z, W);

    internal static Vector4 FromFramework(CNA.Vector4 value) => new(value.X, value.Y, value.Z, value.W);
}
