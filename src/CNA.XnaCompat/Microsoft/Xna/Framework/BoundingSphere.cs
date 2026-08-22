namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.BoundingSphereConverter))]
public struct BoundingSphere : IEquatable<BoundingSphere>
{
    public Vector3 Center;
    public float Radius;

    public BoundingSphere(Vector3 center, float radius)
    {
        if (radius < 0f)
        {
            throw new ArgumentException("The sphere radius must be greater than or equal to zero.");
        }

        Center = center;
        Radius = radius;
    }

    public readonly ContainmentType Contains(Vector3 point) =>
        Vector3.DistanceSquared(Center, point) < Radius * Radius
            ? ContainmentType.Contains
            : ContainmentType.Disjoint;

    public readonly void Contains(ref Vector3 point, out ContainmentType result) => result = Contains(point);

    public readonly ContainmentType Contains(BoundingSphere sphere)
    {
        float distance = Vector3.Distance(Center, sphere.Center);
        float radius = Radius;
        float otherRadius = sphere.Radius;
        if (!(radius + otherRadius >= distance))
        {
            return ContainmentType.Disjoint;
        }

        return radius - otherRadius >= distance
            ? ContainmentType.Contains
            : ContainmentType.Intersects;
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
            Vector3 offset = Center - corner;
            if (offset.LengthSquared() > radiusSquared)
            {
                return ContainmentType.Intersects;
            }
        }

        return ContainmentType.Contains;
    }

    public readonly void Contains(ref BoundingSphere sphere, out ContainmentType result) => result = Contains(sphere);

    public readonly void Contains(ref BoundingBox box, out ContainmentType result) => result = Contains(box);

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

    public readonly bool Intersects(BoundingBox box)
    {
        Vector3 closest = Vector3.Clamp(Center, box.Min, box.Max);
        return !(Vector3.DistanceSquared(Center, closest) > Radius * Radius);
    }

    public readonly bool Intersects(BoundingSphere sphere)
    {
        float distanceSquared = Vector3.DistanceSquared(Center, sphere.Center);
        float radius = Radius;
        float otherRadius = sphere.Radius;
        return (radius * radius) + (2f * radius * otherRadius) + (otherRadius * otherRadius) >
            distanceSquared;
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

    public readonly float? Intersects(Ray ray) => ray.Intersects(this);

    public readonly void Intersects(ref Ray ray, out float? result) => result = Intersects(ray);

    public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
    {
        Vector3 center = Vector3.Lerp(box.Min, box.Max, 0.5f);
        float radius = Vector3.Distance(box.Min, box.Max) * 0.5f;
        return new BoundingSphere(center, radius);
    }

    public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result) =>
        result = CreateFromBoundingBox(box);

    public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
    {
        Vector3 difference = additional.Center - original.Center;
        float distance = difference.Length();
        float radius = original.Radius;
        float otherRadius = additional.Radius;
        if (radius + otherRadius >= distance)
        {
            if (radius - otherRadius >= distance)
            {
                return original;
            }

            if (otherRadius - radius >= distance)
            {
                return additional;
            }
        }

        Vector3 direction = difference * (1f / distance);
        float min = MathHelper.Min(-radius, distance - otherRadius);
        float max = MathHelper.Max(radius, distance + otherRadius);
        float mergedRadius = (max - min) * 0.5f;
        return new BoundingSphere(
            original.Center + (direction * (mergedRadius + min)),
            mergedRadius);
    }

    public static void CreateMerged(
        ref BoundingSphere original,
        ref BoundingSphere additional,
        out BoundingSphere result) => result = CreateMerged(original, additional);

    public static bool operator ==(BoundingSphere a, BoundingSphere b) => a.Equals(b);
    public static bool operator !=(BoundingSphere a, BoundingSphere b) => !a.Equals(b);

    public readonly bool Equals(BoundingSphere other) => Center == other.Center && Radius == other.Radius;
    public override readonly bool Equals(object? obj) => obj is BoundingSphere other && Equals(other);
    public override readonly int GetHashCode() => Center.GetHashCode() + Radius.GetHashCode();
    public override readonly string ToString() => $"{{Center:{Center} Radius:{Radius}}}";

    internal readonly void SupportMapping(ref Vector3 direction, out Vector3 result)
    {
        float length = direction.Length();
        float scale = Radius / length;
        result.X = Center.X + (direction.X * scale);
        result.Y = Center.Y + (direction.Y * scale);
        result.Z = Center.Z + (direction.Z * scale);
    }

    internal readonly CNA.BoundingSphere ToFramework() => new(Center.ToFramework(), Radius);

    internal static BoundingSphere FromFramework(CNA.BoundingSphere value) =>
        new(Vector3.FromFramework(value.Center), value.Radius);

    /// <summary>Matches XNA's two-pass Ritter-style approximation rather than computing the
    /// minimal enclosing sphere.</summary>
    public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        IEnumerator<Vector3> enumerator = points.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            throw new ArgumentException("The point sequence must contain at least one point.");
        }

        Vector3 minX = enumerator.Current;
        Vector3 maxX = minX;
        Vector3 minY = minX;
        Vector3 maxY = minX;
        Vector3 minZ = minX;
        Vector3 maxZ = minX;
        foreach (Vector3 point in points)
        {
            if (point.X < minX.X) minX = point;
            if (point.X > maxX.X) maxX = point;
            if (point.Y < minY.Y) minY = point;
            if (point.Y > maxY.Y) maxY = point;
            if (point.Z < minZ.Z) minZ = point;
            if (point.Z > maxZ.Z) maxZ = point;
        }

        float distanceX = Vector3.Distance(maxX, minX);
        float distanceY = Vector3.Distance(maxY, minY);
        float distanceZ = Vector3.Distance(maxZ, minZ);
        Vector3 center;
        float radius;
        if (distanceX > distanceY)
        {
            if (distanceX > distanceZ)
            {
                center = Vector3.Lerp(maxX, minX, 0.5f);
                radius = distanceX * 0.5f;
            }
            else
            {
                center = Vector3.Lerp(maxZ, minZ, 0.5f);
                radius = distanceZ * 0.5f;
            }
        }
        else if (distanceY > distanceZ)
        {
            center = Vector3.Lerp(maxY, minY, 0.5f);
            radius = distanceY * 0.5f;
        }
        else
        {
            center = Vector3.Lerp(maxZ, minZ, 0.5f);
            radius = distanceZ * 0.5f;
        }

        foreach (Vector3 point in points)
        {
            Vector3 offset = point - center;
            float distance = offset.Length();
            if (distance > radius)
            {
                radius = (radius + distance) * 0.5f;
                center += (1f - (radius / distance)) * offset;
            }
        }

        return new BoundingSphere(center, radius);
    }

    /// <summary>Matches real XNA's <c>CreateFromFrustum</c>.</summary>
    public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return CreateFromPoints(frustum.GetCorners());
    }

    /// <summary>Matches real XNA's <c>Transform</c>.</summary>
    public readonly BoundingSphere Transform(Matrix matrix)
    {
        float row1LengthSquared = (matrix.M11 * matrix.M11) +
            (matrix.M12 * matrix.M12) +
            (matrix.M13 * matrix.M13);
        float row2LengthSquared = (matrix.M21 * matrix.M21) +
            (matrix.M22 * matrix.M22) +
            (matrix.M23 * matrix.M23);
        float row3LengthSquared = (matrix.M31 * matrix.M31) +
            (matrix.M32 * matrix.M32) +
            (matrix.M33 * matrix.M33);
        float maximumLengthSquared = Math.Max(
            row1LengthSquared,
            Math.Max(row2LengthSquared, row3LengthSquared));
        return new BoundingSphere(
            Vector3.Transform(Center, matrix),
            Radius * (float)Math.Sqrt(maximumLengthSquared));
    }

    public readonly void Transform(ref Matrix matrix, out BoundingSphere result) => result = Transform(matrix);
}
