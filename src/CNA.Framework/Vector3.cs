namespace CNA.Framework;

/// <summary>Local, managed 3D vector -- see the rationale in Vector2.cs.</summary>
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

    public Vector3(Vector2 xy, float z)
    {
        X = xy.X;
        Y = xy.Y;
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

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

    public void Normalize()
    {
        float length = Length();
        if (length >= float.Epsilon)
        {
            X /= length;
            Y /= length;
            Z /= length;
        }
    }

    public static Vector3 Normalize(Vector3 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector3 a, Vector3 b) => (a - b).Length();

    public static float DistanceSquared(Vector3 a, Vector3 b) => (a - b).LengthSquared();

    public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3 Cross(Vector3 a, Vector3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        float dot = Dot(vector, normal);
        return vector - (2f * dot * normal);
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float amount) => new(
        a.X + ((b.X - a.X) * amount),
        a.Y + ((b.Y - a.Y) * amount),
        a.Z + ((b.Z - a.Z) * amount));

    public static Vector3 Min(Vector3 a, Vector3 b) => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z));

    public static Vector3 Max(Vector3 a, Vector3 b) => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z));

    public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => Min(Max(value, min), max);

    public static Vector3 Transform(Vector3 position, Matrix matrix) => new(
        (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
        (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
        (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);

    public static Vector3 TransformNormal(Vector3 normal, Matrix matrix) => new(
        (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
        (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
        (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33));

    public static Vector3 Transform(Vector3 value, Quaternion rotation)
    {
        Quaternion conjugate = Quaternion.Conjugate(rotation);
        Quaternion result = rotation * new Quaternion(value.X, value.Y, value.Z, 0f) * conjugate;
        return new Vector3(result.X, result.Y, result.Z);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);
    public static Vector3 operator *(Vector3 a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3 operator *(float scalar, Vector3 a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3 operator /(Vector3 a, float scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);
    public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);

    public readonly bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override readonly bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z}}}";
}
