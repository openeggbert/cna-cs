namespace CNA.Framework;

/// <summary>Local, managed 4D vector -- see the rationale in Vector2.cs.</summary>
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

    public Vector4(Vector3 xyz, float w)
    {
        X = xyz.X;
        Y = xyz.Y;
        Z = xyz.Z;
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

    public static Vector4 Normalize(Vector4 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector4 a, Vector4 b) => (a - b).Length();

    public static float Dot(Vector4 a, Vector4 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);

    public static Vector4 Lerp(Vector4 a, Vector4 b, float amount) => new(
        a.X + ((b.X - a.X) * amount),
        a.Y + ((b.Y - a.Y) * amount),
        a.Z + ((b.Z - a.Z) * amount),
        a.W + ((b.W - a.W) * amount));

    public static Vector4 Min(Vector4 a, Vector4 b) =>
        new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z), MathF.Min(a.W, b.W));

    public static Vector4 Max(Vector4 a, Vector4 b) =>
        new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z), MathF.Max(a.W, b.W));

    public static Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max) => Min(Max(value, min), max);

    public static Vector4 Transform(Vector4 vector, Matrix matrix) => new(
        (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + (vector.W * matrix.M41),
        (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + (vector.W * matrix.M42),
        (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + (vector.W * matrix.M43),
        (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + (vector.W * matrix.M44));

    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Vector4 operator -(Vector4 value) => new(-value.X, -value.Y, -value.Z, -value.W);
    public static Vector4 operator *(Vector4 a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar, a.W * scalar);
    public static Vector4 operator *(float scalar, Vector4 a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar, a.W * scalar);
    public static Vector4 operator /(Vector4 a, float scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar, a.W / scalar);

    public static bool operator ==(Vector4 a, Vector4 b) => a.Equals(b);
    public static bool operator !=(Vector4 a, Vector4 b) => !a.Equals(b);

    public readonly bool Equals(Vector4 other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    public override readonly bool Equals(object? obj) => obj is Vector4 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";
}
