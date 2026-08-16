namespace Microsoft.Xna.Framework;

public struct BoundingSphere : IEquatable<BoundingSphere>
{
    public Vector3 Center;
    public float Radius;

    public BoundingSphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public readonly bool Contains(Vector3 point) => ((CNA.BoundingSphere)this).Contains(point);

    public readonly ContainmentType Contains(BoundingSphere sphere) => ((CNA.BoundingSphere)this).Contains(sphere).ToCompat();

    public readonly ContainmentType Contains(BoundingBox box) => ((CNA.BoundingSphere)this).Contains(box).ToCompat();

    public readonly bool Intersects(BoundingBox box) => ((CNA.BoundingSphere)this).Intersects(box);

    public readonly bool Intersects(BoundingSphere sphere) => ((CNA.BoundingSphere)this).Intersects(sphere);

    public readonly float? Intersects(Ray ray) => ((CNA.BoundingSphere)this).Intersects(ray);

    public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
    {
        CNA.BoundingSphere result = CNA.BoundingSphere.CreateFromBoundingBox(box);
        return new BoundingSphere(result.Center, result.Radius);
    }

    public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
    {
        CNA.BoundingSphere result = CNA.BoundingSphere.CreateMerged(original, additional);
        return new BoundingSphere(result.Center, result.Radius);
    }

    public static bool operator ==(BoundingSphere a, BoundingSphere b) => a.Equals(b);
    public static bool operator !=(BoundingSphere a, BoundingSphere b) => !a.Equals(b);

    public readonly bool Equals(BoundingSphere other) => Center.Equals(other.Center) && Radius.Equals(other.Radius);
    public override readonly bool Equals(object? obj) => obj is BoundingSphere other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Center, Radius);
    public override readonly string ToString() => $"{{Center:{Center} Radius:{Radius}}}";

    public static implicit operator CNA.BoundingSphere(BoundingSphere value) => new(value.Center, value.Radius);
    public static implicit operator BoundingSphere(CNA.BoundingSphere value) => new(value.Center, value.Radius);
}
