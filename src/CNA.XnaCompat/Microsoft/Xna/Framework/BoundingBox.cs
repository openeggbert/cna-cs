namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.BoundingBoxConverter))]
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
        new(Min.X, Max.Y, Max.Z),
        new(Max.X, Max.Y, Max.Z),
        new(Max.X, Min.Y, Max.Z),
        new(Min.X, Min.Y, Max.Z),
        new(Min.X, Max.Y, Min.Z),
        new(Max.X, Max.Y, Min.Z),
        new(Max.X, Min.Y, Min.Z),
        new(Min.X, Min.Y, Min.Z),
    ];

    public readonly void GetCorners(Vector3[] corners)
    {
        ArgumentNullException.ThrowIfNull(corners);
        if (corners.Length < CornerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(corners));
        }

        GetCorners().CopyTo(corners, 0);
    }

    public readonly ContainmentType Contains(Vector3 point) =>
        Min.X <= point.X && point.X <= Max.X &&
        Min.Y <= point.Y && point.Y <= Max.Y &&
        Min.Z <= point.Z && point.Z <= Max.Z
            ? ContainmentType.Contains
            : ContainmentType.Disjoint;

    public readonly void Contains(ref Vector3 point, out ContainmentType result) => result = Contains(point);

    public readonly ContainmentType Contains(BoundingBox box)
    {
        if (Max.X < box.Min.X || Min.X > box.Max.X ||
            Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
            Max.Z < box.Min.Z || Min.Z > box.Max.Z)
        {
            return ContainmentType.Disjoint;
        }

        return Min.X <= box.Min.X && box.Max.X <= Max.X &&
            Min.Y <= box.Min.Y && box.Max.Y <= Max.Y &&
            Min.Z <= box.Min.Z && box.Max.Z <= Max.Z
                ? ContainmentType.Contains
                : ContainmentType.Intersects;
    }

    public readonly ContainmentType Contains(BoundingSphere sphere)
    {
        Vector3 closest = Vector3.Clamp(sphere.Center, Min, Max);
        float distanceSquared = Vector3.DistanceSquared(sphere.Center, closest);
        float radius = sphere.Radius;
        if (distanceSquared > radius * radius)
        {
            return ContainmentType.Disjoint;
        }

        // Preserve XNA 4.0's observable width checks, including its repeated X-width check for Z.
        return Min.X + radius <= sphere.Center.X && sphere.Center.X <= Max.X - radius &&
            Max.X - Min.X > radius &&
            Min.Y + radius <= sphere.Center.Y && sphere.Center.Y <= Max.Y - radius &&
            Max.Y - Min.Y > radius &&
            Min.Z + radius <= sphere.Center.Z && sphere.Center.Z <= Max.Z - radius &&
            Max.X - Min.X > radius
                ? ContainmentType.Contains
                : ContainmentType.Intersects;
    }

    public readonly void Contains(ref BoundingBox box, out ContainmentType result) => result = Contains(box);

    public readonly void Contains(ref BoundingSphere sphere, out ContainmentType result) => result = Contains(sphere);

    public readonly ContainmentType Contains(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        bool allInside = true;
        foreach (Vector3 corner in frustum.GetCorners())
        {
            if (Contains(corner) == ContainmentType.Disjoint)
            {
                allInside = false;
                break;
            }
        }

        return allInside
            ? ContainmentType.Contains
            : Intersects(frustum) ? ContainmentType.Intersects : ContainmentType.Disjoint;
    }

    public readonly bool Intersects(BoundingBox box) =>
        !(Max.X < box.Min.X || Min.X > box.Max.X ||
          Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
          Max.Z < box.Min.Z || Min.Z > box.Max.Z);

    public readonly bool Intersects(BoundingSphere sphere)
    {
        Vector3 closest = Vector3.Clamp(sphere.Center, Min, Max);
        return !(Vector3.DistanceSquared(sphere.Center, closest) > sphere.Radius * sphere.Radius);
    }

    public readonly bool Intersects(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return frustum.Intersects(this);
    }

    public readonly PlaneIntersectionType Intersects(Plane plane) => plane.Intersects(this);

    public readonly void Intersects(ref BoundingBox box, out bool result) => result = Intersects(box);

    public readonly void Intersects(ref BoundingSphere sphere, out bool result) => result = Intersects(sphere);

    public readonly void Intersects(ref Plane plane, out PlaneIntersectionType result) => result = Intersects(plane);

    public readonly float? Intersects(Ray ray)
    {
        const float epsilon = 1e-6f;
        float distance = 0f;
        float maxDistance = float.MaxValue;

        if (!IntersectSlab(ray.Position.X, ray.Direction.X, Min.X, Max.X, epsilon, ref distance, ref maxDistance) ||
            !IntersectSlab(ray.Position.Y, ray.Direction.Y, Min.Y, Max.Y, epsilon, ref distance, ref maxDistance) ||
            !IntersectSlab(ray.Position.Z, ray.Direction.Z, Min.Z, Max.Z, epsilon, ref distance, ref maxDistance))
        {
            return null;
        }

        return distance;
    }

    public readonly void Intersects(ref Ray ray, out float? result) => result = Intersects(ray);

    public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
    {
        if (points is null)
        {
            throw new ArgumentNullException();
        }

        bool hasPoint = false;
        Vector3 min = new(float.MaxValue);
        Vector3 max = new(float.MinValue);
        foreach (Vector3 point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
            hasPoint = true;
        }

        if (!hasPoint)
        {
            throw new ArgumentException("The point sequence must contain at least one point.");
        }

        return new BoundingBox(min, max);
    }

    public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional) =>
        new(Vector3.Min(original.Min, additional.Min), Vector3.Max(original.Max, additional.Max));

    public static void CreateMerged(
        ref BoundingBox original,
        ref BoundingBox additional,
        out BoundingBox result) => result = CreateMerged(original, additional);

    public static BoundingBox CreateFromSphere(BoundingSphere sphere) => new(
        new Vector3(
            sphere.Center.X - sphere.Radius,
            sphere.Center.Y - sphere.Radius,
            sphere.Center.Z - sphere.Radius),
        new Vector3(
            sphere.Center.X + sphere.Radius,
            sphere.Center.Y + sphere.Radius,
            sphere.Center.Z + sphere.Radius));

    public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBox result) =>
        result = CreateFromSphere(sphere);

    public static bool operator ==(BoundingBox a, BoundingBox b) => a.Equals(b);
    public static bool operator !=(BoundingBox a, BoundingBox b) => !a.Equals(b);

    public readonly bool Equals(BoundingBox other) => Min == other.Min && Max == other.Max;
    public override readonly bool Equals(object? obj) => obj is BoundingBox other && Equals(other);
    public override readonly int GetHashCode() => Min.GetHashCode() + Max.GetHashCode();
    public override readonly string ToString() => $"{{Min:{Min} Max:{Max}}}";

    internal readonly void SupportMapping(ref Vector3 direction, out Vector3 result)
    {
        result.X = direction.X >= 0f ? Max.X : Min.X;
        result.Y = direction.Y >= 0f ? Max.Y : Min.Y;
        result.Z = direction.Z >= 0f ? Max.Z : Min.Z;
    }

    internal readonly CNA.BoundingBox ToFramework() => new(Min.ToFramework(), Max.ToFramework());

    internal static BoundingBox FromFramework(CNA.BoundingBox value) =>
        new(Vector3.FromFramework(value.Min), Vector3.FromFramework(value.Max));

    private static bool IntersectSlab(
        float position,
        float direction,
        float min,
        float max,
        float epsilon,
        ref float distance,
        ref float maxDistance)
    {
        if (Math.Abs(direction) < epsilon)
        {
            return position >= min && position <= max;
        }

        float inverseDirection = 1f / direction;
        float near = (min - position) * inverseDirection;
        float far = (max - position) * inverseDirection;
        if (near > far)
        {
            (near, far) = (far, near);
        }

        distance = MathHelper.Max(near, distance);
        maxDistance = MathHelper.Min(far, maxDistance);
        return distance <= maxDistance;
    }
}
