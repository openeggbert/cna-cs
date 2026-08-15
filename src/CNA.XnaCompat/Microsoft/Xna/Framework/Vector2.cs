namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Vector2</c>. Structs cannot inherit, so this is a small, deliberately
/// duplicated struct with implicit conversions to/from <see cref="CNA.Framework.Vector2"/> rather
/// than a subclass -- see docs/architecture.md ("Why the XNA value types are not literally the
/// same type as the CNA.Framework ones"). A future codegen tool
/// (tools/binding-generator/) is the intended long-term fix for this duplication.
/// </summary>
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

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y);

    public readonly float LengthSquared() => X * X + Y * Y;

    public void Normalize()
    {
        float length = Length();
        if (length >= float.Epsilon)
        {
            X /= length;
            Y /= length;
        }
    }

    public static Vector2 Normalize(Vector2 value)
    {
        value.Normalize();
        return value;
    }

    public static float Distance(Vector2 a, Vector2 b) => (a - b).Length();

    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator *(float scalar, Vector2 a) => new(a.X * scalar, a.Y * scalar);
    public static Vector2 operator /(Vector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);

    public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
    public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);

    public readonly bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override readonly bool Equals(object? obj) => obj is Vector2 other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{{X:{X} Y:{Y}}}";

    public static implicit operator CNA.Framework.Vector2(Vector2 value) => new(value.X, value.Y);
    public static implicit operator Vector2(CNA.Framework.Vector2 value) => new(value.X, value.Y);
}
