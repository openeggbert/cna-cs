namespace CNA.Framework;

public struct BoundingSphere : IEquatable<BoundingSphere>
{
    public Vector3 Center;
    public float Radius;

    public BoundingSphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public readonly bool Contains(Vector3 point) => Vector3.DistanceSquared(Center, point) <= Radius * Radius;

    public readonly ContainmentType Contains(BoundingSphere sphere)
    {
        float distance = Vector3.Distance(Center, sphere.Center);

        if (distance > Radius + sphere.Radius)
        {
            return ContainmentType.Disjoint;
        }

        return distance + sphere.Radius <= Radius ? ContainmentType.Contains : ContainmentType.Intersects;
    }

    public readonly ContainmentType Contains(BoundingBox box)
    {
        if (!box.Intersects(this))
        {
            return ContainmentType.Disjoint;
        }

        float radiusSquared = Radius * Radius;
        foreach (Vector3 corner in box.GetCorners())
        {
            if (Vector3.DistanceSquared(Center, corner) > radiusSquared)
            {
                return ContainmentType.Intersects;
            }
        }

        return ContainmentType.Contains;
    }

    public readonly bool Intersects(BoundingBox box) => box.Intersects(this);

    public readonly bool Intersects(BoundingSphere sphere)
    {
        float radiusSum = Radius + sphere.Radius;
        return Vector3.DistanceSquared(Center, sphere.Center) <= radiusSum * radiusSum;
    }

    public readonly float? Intersects(Ray ray)
    {
        Vector3 originToCenter = Center - ray.Position;
        float distanceSquaredToCenter = originToCenter.LengthSquared();
        float radiusSquared = Radius * Radius;

        if (distanceSquaredToCenter <= radiusSquared)
        {
            return 0f;
        }

        float projection = Vector3.Dot(originToCenter, ray.Direction);
        if (projection < 0f)
        {
            return null;
        }

        float distanceSquaredFromClosestPoint = distanceSquaredToCenter - (projection * projection);
        if (distanceSquaredFromClosestPoint > radiusSquared)
        {
            return null;
        }

        float halfChord = MathF.Sqrt(radiusSquared - distanceSquaredFromClosestPoint);
        return projection - halfChord;
    }

    public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
    {
        Vector3 center = (box.Min + box.Max) * 0.5f;
        float radius = Vector3.Distance(center, box.Max);
        return new BoundingSphere(center, radius);
    }

    public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
    {
        Vector3 direction = additional.Center - original.Center;
        float distance = direction.Length();

        if (distance <= original.Radius + additional.Radius)
        {
            if (distance <= original.Radius - additional.Radius)
            {
                return original;
            }

            if (distance <= additional.Radius - original.Radius)
            {
                return additional;
            }
        }

        float radius = (distance + original.Radius + additional.Radius) * 0.5f;
        Vector3 center = distance > float.Epsilon
            ? original.Center + (direction * ((radius - original.Radius) / distance))
            : original.Center;
        return new BoundingSphere(center, radius);
    }

    public static bool operator ==(BoundingSphere a, BoundingSphere b) => a.Equals(b);
    public static bool operator !=(BoundingSphere a, BoundingSphere b) => !a.Equals(b);

    public readonly bool Equals(BoundingSphere other) => Center.Equals(other.Center) && Radius.Equals(other.Radius);
    public override readonly bool Equals(object? obj) => obj is BoundingSphere other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Center, Radius);
    public override readonly string ToString() => $"{{Center:{Center} Radius:{Radius}}}";
}
