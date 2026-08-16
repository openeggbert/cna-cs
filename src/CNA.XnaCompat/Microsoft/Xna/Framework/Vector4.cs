namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>Vector4</c>. See Vector3.cs for the duplicate-storage,
/// delegate-behavior pattern used here.</summary>
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

    public readonly float Length() => ((CNA.Vector4)this).Length();

    public readonly float LengthSquared() => ((CNA.Vector4)this).LengthSquared();

    public void Normalize()
    {
        CNA.Vector4 value = this;
        value.Normalize();
        this = value;
    }

    public static Vector4 Normalize(Vector4 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector4 a, Vector4 b) => CNA.Vector4.Distance(a, b);

    public static float Dot(Vector4 a, Vector4 b) => CNA.Vector4.Dot(a, b);

    public static Vector4 Lerp(Vector4 a, Vector4 b, float amount) => CNA.Vector4.Lerp(a, b, amount);

    public static Vector4 Min(Vector4 a, Vector4 b) => CNA.Vector4.Min(a, b);

    public static Vector4 Max(Vector4 a, Vector4 b) => CNA.Vector4.Max(a, b);

    public static Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max) => CNA.Vector4.Clamp(value, min, max);

    public static Vector4 Transform(Vector4 vector, Matrix matrix) => CNA.Vector4.Transform(vector, matrix);

    public static Vector4 operator +(Vector4 a, Vector4 b) => (CNA.Vector4)a + (CNA.Vector4)b;
    public static Vector4 operator -(Vector4 a, Vector4 b) => (CNA.Vector4)a - (CNA.Vector4)b;
    public static Vector4 operator -(Vector4 value) => -(CNA.Vector4)value;
    public static Vector4 operator *(Vector4 a, float scalar) => (CNA.Vector4)a * scalar;
    public static Vector4 operator *(float scalar, Vector4 a) => scalar * (CNA.Vector4)a;
    public static Vector4 operator /(Vector4 a, float scalar) => (CNA.Vector4)a / scalar;

    public static bool operator ==(Vector4 a, Vector4 b) => a.Equals(b);
    public static bool operator !=(Vector4 a, Vector4 b) => !a.Equals(b);

    public readonly bool Equals(Vector4 other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    public override readonly bool Equals(object? obj) => obj is Vector4 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";

    public static implicit operator CNA.Vector4(Vector4 value) => new(value.X, value.Y, value.Z, value.W);
    public static implicit operator Vector4(CNA.Vector4 value) => new(value.X, value.Y, value.Z, value.W);
}
