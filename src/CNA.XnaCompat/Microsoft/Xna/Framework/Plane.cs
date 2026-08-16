namespace Microsoft.Xna.Framework;

public struct Plane : IEquatable<Plane>
{
    public Vector3 Normal;
    public float D;

    public Plane(Vector3 normal, float d)
    {
        Normal = normal;
        D = d;
    }

    public Plane(float a, float b, float c, float d)
    {
        Normal = new Vector3(a, b, c);
        D = d;
    }

    public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        CNA.Framework.Plane value = new(point1, point2, point3);
        Normal = value.Normal;
        D = value.D;
    }

    public readonly float DotCoordinate(Vector3 value) => ((CNA.Framework.Plane)this).DotCoordinate(value);

    public readonly float DotNormal(Vector3 value) => ((CNA.Framework.Plane)this).DotNormal(value);

    public void Normalize()
    {
        CNA.Framework.Plane value = this;
        value.Normalize();
        Normal = value.Normal;
        D = value.D;
    }

    public static Plane Normalize(Plane value)
    {
        value.Normalize();
        return value;
    }

    public readonly PlaneIntersectionType Intersects(BoundingSphere sphere) =>
        ((CNA.Framework.Plane)this).Intersects(sphere).ToCompat();

    public readonly PlaneIntersectionType Intersects(BoundingBox box) =>
        ((CNA.Framework.Plane)this).Intersects(box).ToCompat();

    public static bool operator ==(Plane a, Plane b) => a.Equals(b);
    public static bool operator !=(Plane a, Plane b) => !a.Equals(b);

    public readonly bool Equals(Plane other) => Normal.Equals(other.Normal) && D.Equals(other.D);
    public override readonly bool Equals(object? obj) => obj is Plane other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Normal, D);
    public override readonly string ToString() => $"{{Normal:{Normal} D:{D}}}";

    public static implicit operator CNA.Framework.Plane(Plane value) => new(value.Normal, value.D);
    public static implicit operator Plane(CNA.Framework.Plane value) => new(value.Normal, value.D);
}
