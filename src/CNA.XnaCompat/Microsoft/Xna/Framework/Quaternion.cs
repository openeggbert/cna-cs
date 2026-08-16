namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>Quaternion</c>. See Vector3.cs for the duplicate-storage,
/// delegate-behavior pattern used here.</summary>
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

    public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle) =>
        CNA.Quaternion.CreateFromAxisAngle(axis, angle);

    public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll) =>
        CNA.Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

    public static Quaternion CreateFromRotationMatrix(Matrix matrix) => CNA.Quaternion.CreateFromRotationMatrix(matrix);

    public readonly float Length() => ((CNA.Quaternion)this).Length();

    public readonly float LengthSquared() => ((CNA.Quaternion)this).LengthSquared();

    public void Normalize()
    {
        CNA.Quaternion value = this;
        value.Normalize();
        this = value;
    }

    public static Quaternion Normalize(Quaternion value)
    {
        value.Normalize();
        return value;
    }

    public static Quaternion Conjugate(Quaternion value) => CNA.Quaternion.Conjugate(value);

    public static Quaternion Inverse(Quaternion value) => CNA.Quaternion.Inverse(value);

    public static float Dot(Quaternion a, Quaternion b) => CNA.Quaternion.Dot(a, b);

    public static Quaternion Concatenate(Quaternion value1, Quaternion value2) =>
        CNA.Quaternion.Concatenate(value1, value2);

    public static Quaternion Lerp(Quaternion a, Quaternion b, float amount) => CNA.Quaternion.Lerp(a, b, amount);

    public static Quaternion Slerp(Quaternion a, Quaternion b, float amount) => CNA.Quaternion.Slerp(a, b, amount);

    public static Quaternion operator *(Quaternion a, Quaternion b) => (CNA.Quaternion)a * (CNA.Quaternion)b;
    public static Quaternion operator +(Quaternion a, Quaternion b) => (CNA.Quaternion)a + (CNA.Quaternion)b;
    public static Quaternion operator -(Quaternion a, Quaternion b) => (CNA.Quaternion)a - (CNA.Quaternion)b;
    public static Quaternion operator -(Quaternion value) => -(CNA.Quaternion)value;

    public static bool operator ==(Quaternion a, Quaternion b) => a.Equals(b);
    public static bool operator !=(Quaternion a, Quaternion b) => !a.Equals(b);

    public readonly bool Equals(Quaternion other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    public override readonly bool Equals(object? obj) => obj is Quaternion other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";

    public static implicit operator CNA.Quaternion(Quaternion value) => new(value.X, value.Y, value.Z, value.W);
    public static implicit operator Quaternion(CNA.Quaternion value) => new(value.X, value.Y, value.Z, value.W);
}
