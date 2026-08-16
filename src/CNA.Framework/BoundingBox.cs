namespace CNA.Framework;

public struct BoundingBox : IEquatable<BoundingBox>
{
    public const int CornerCount = 8;

    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public readonly Vector3[] GetCorners() =>
    [
        new Vector3(Min.X, Max.Y, Max.Z),
        new Vector3(Max.X, Max.Y, Max.Z),
        new Vector3(Max.X, Min.Y, Max.Z),
        new Vector3(Min.X, Min.Y, Max.Z),
        new Vector3(Min.X, Max.Y, Min.Z),
        new Vector3(Max.X, Max.Y, Min.Z),
        new Vector3(Max.X, Min.Y, Min.Z),
        new Vector3(Min.X, Min.Y, Min.Z),
    ];

    public readonly bool Contains(Vector3 point) =>
        point.X >= Min.X && point.X <= Max.X &&
        point.Y >= Min.Y && point.Y <= Max.Y &&
        point.Z >= Min.Z && point.Z <= Max.Z;

    public readonly ContainmentType Contains(BoundingBox box)
    {
        if (Max.X < box.Min.X || Min.X > box.Max.X ||
            Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
            Max.Z < box.Min.Z || Min.Z > box.Max.Z)
        {
            return ContainmentType.Disjoint;
        }

        bool fullyInside =
            Min.X <= box.Min.X && box.Max.X <= Max.X &&
            Min.Y <= box.Min.Y && box.Max.Y <= Max.Y &&
            Min.Z <= box.Min.Z && box.Max.Z <= Max.Z;

        return fullyInside ? ContainmentType.Contains : ContainmentType.Intersects;
    }

    public readonly ContainmentType Contains(BoundingSphere sphere)
    {
        Vector3 clamped = Vector3.Clamp(sphere.Center, Min, Max);
        float distanceSquared = Vector3.DistanceSquared(sphere.Center, clamped);

        if (distanceSquared > sphere.Radius * sphere.Radius)
        {
            return ContainmentType.Disjoint;
        }

        bool fullyInside =
            Min.X + sphere.Radius <= sphere.Center.X && sphere.Center.X <= Max.X - sphere.Radius && Max.X - Min.X > sphere.Radius * 2f &&
            Min.Y + sphere.Radius <= sphere.Center.Y && sphere.Center.Y <= Max.Y - sphere.Radius && Max.Y - Min.Y > sphere.Radius * 2f &&
            Min.Z + sphere.Radius <= sphere.Center.Z && sphere.Center.Z <= Max.Z - sphere.Radius && Max.Z - Min.Z > sphere.Radius * 2f;

        return fullyInside ? ContainmentType.Contains : ContainmentType.Intersects;
    }

    public readonly bool Intersects(BoundingBox box) =>
        Max.X >= box.Min.X && Min.X <= box.Max.X &&
        Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
        Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;

    public readonly bool Intersects(BoundingSphere sphere)
    {
        Vector3 clamped = Vector3.Clamp(sphere.Center, Min, Max);
        return Vector3.DistanceSquared(sphere.Center, clamped) <= sphere.Radius * sphere.Radius;
    }

    public readonly float? Intersects(Ray ray)
    {
        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        if (!Slab(ray.Position.X, ray.Direction.X, Min.X, Max.X, ref tMin, ref tMax) ||
            !Slab(ray.Position.Y, ray.Direction.Y, Min.Y, Max.Y, ref tMin, ref tMax) ||
            !Slab(ray.Position.Z, ray.Direction.Z, Min.Z, Max.Z, ref tMin, ref tMax))
        {
            return null;
        }

        if (tMax < 0f)
        {
            return null;
        }

        return tMin < 0f ? 0f : tMin;

        static bool Slab(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
        {
            if (MathF.Abs(direction) < float.Epsilon)
            {
                return origin >= min && origin <= max;
            }

            float inverseDirection = 1f / direction;
            float t1 = (min - origin) * inverseDirection;
            float t2 = (max - origin) * inverseDirection;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
            }

            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            return tMin <= tMax;
        }
    }

    public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        Vector3 min = new(float.MaxValue);
        Vector3 max = new(float.MinValue);
        bool any = false;

        foreach (Vector3 point in points)
        {
            any = true;
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        if (!any)
        {
            throw new ArgumentException("The point sequence must not be empty.", nameof(points));
        }

        return new BoundingBox(min, max);
    }

    public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional) =>
        new(Vector3.Min(original.Min, additional.Min), Vector3.Max(original.Max, additional.Max));

    public static BoundingBox CreateFromSphere(BoundingSphere sphere) =>
        new(sphere.Center - new Vector3(sphere.Radius), sphere.Center + new Vector3(sphere.Radius));

    public static bool operator ==(BoundingBox a, BoundingBox b) => a.Equals(b);
    public static bool operator !=(BoundingBox a, BoundingBox b) => !a.Equals(b);

    public readonly bool Equals(BoundingBox other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override readonly bool Equals(object? obj) => obj is BoundingBox other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);
    public override readonly string ToString() => $"{{Min:{Min} Max:{Max}}}";
}
