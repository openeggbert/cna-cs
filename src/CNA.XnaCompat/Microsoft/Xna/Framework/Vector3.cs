namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Vector3</c>. Storage (fields) is duplicated like Vector2.cs/Color.cs,
/// but all behavior delegates to <see cref="CNA.Vector3"/> through the implicit
/// conversion operators rather than re-deriving the formulas a second time -- see
/// docs/architecture.md.
/// </summary>
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

    public readonly float Length() => ((CNA.Vector3)this).Length();

    public readonly float LengthSquared() => ((CNA.Vector3)this).LengthSquared();

    public void Normalize()
    {
        CNA.Vector3 value = this;
        value.Normalize();
        this = value;
    }

    public static Vector3 Normalize(Vector3 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector3 a, Vector3 b) => CNA.Vector3.Distance(a, b);

    public static float DistanceSquared(Vector3 a, Vector3 b) => CNA.Vector3.DistanceSquared(a, b);

    public static float Dot(Vector3 a, Vector3 b) => CNA.Vector3.Dot(a, b);

    public static Vector3 Cross(Vector3 a, Vector3 b) => CNA.Vector3.Cross(a, b);

    public static Vector3 Reflect(Vector3 vector, Vector3 normal) => CNA.Vector3.Reflect(vector, normal);

    public static Vector3 Lerp(Vector3 a, Vector3 b, float amount) => CNA.Vector3.Lerp(a, b, amount);

    public static Vector3 Min(Vector3 a, Vector3 b) => CNA.Vector3.Min(a, b);

    public static Vector3 Max(Vector3 a, Vector3 b) => CNA.Vector3.Max(a, b);

    public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => CNA.Vector3.Clamp(value, min, max);

    public static Vector3 Transform(Vector3 position, Matrix matrix) => CNA.Vector3.Transform(position, matrix);

    public static Vector3 TransformNormal(Vector3 normal, Matrix matrix) => CNA.Vector3.TransformNormal(normal, matrix);

    public static Vector3 Transform(Vector3 value, Quaternion rotation) => CNA.Vector3.Transform(value, rotation);

    public static Vector3 operator +(Vector3 a, Vector3 b) => (CNA.Vector3)a + (CNA.Vector3)b;
    public static Vector3 operator -(Vector3 a, Vector3 b) => (CNA.Vector3)a - (CNA.Vector3)b;
    public static Vector3 operator -(Vector3 value) => -(CNA.Vector3)value;
    public static Vector3 operator *(Vector3 a, float scalar) => (CNA.Vector3)a * scalar;
    public static Vector3 operator *(float scalar, Vector3 a) => scalar * (CNA.Vector3)a;
    public static Vector3 operator *(Vector3 a, Vector3 b) => (CNA.Vector3)a * (CNA.Vector3)b;
    public static Vector3 operator /(Vector3 a, float scalar) => (CNA.Vector3)a / scalar;

    public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);
    public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);

    public readonly bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override readonly bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z}}}";

    public static implicit operator CNA.Vector3(Vector3 value) => new(value.X, value.Y, value.Z);
    public static implicit operator Vector3(CNA.Vector3 value) => new(value.X, value.Y, value.Z);
}
