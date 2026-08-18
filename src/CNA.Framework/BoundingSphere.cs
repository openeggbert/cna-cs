namespace CNA;

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

    /// <summary>
    /// The smallest sphere this algorithm finds around <paramref name="points"/>.
    ///
    /// Ritter's approximation, ported from the engine's own <c>BoundingSphere.cpp</c>: pick the pair
    /// of points furthest apart along whichever axis spreads widest, take that as the initial
    /// diameter, then grow the sphere over any point still outside it. Deliberately <em>not</em> the
    /// minimal enclosing sphere -- that is a different, more expensive algorithm, and matching XNA
    /// here means matching the approximation, since a caller comparing radii against XNA's own
    /// output would otherwise see them differ.
    /// </summary>
    public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        Vector3 minX = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxX = -minX;
        Vector3 minY = minX, maxY = maxX, minZ = minX, maxZ = maxX;

        // Materialised because the algorithm needs two passes and the argument is an enumerable.
        var all = points as IReadOnlyList<Vector3> ?? [.. points];
        if (all.Count == 0)
        {
            throw new ArgumentException("points must contain at least one point.", nameof(points));
        }

        foreach (Vector3 point in all)
        {
            if (point.X < minX.X) minX = point;
            if (point.X > maxX.X) maxX = point;
            if (point.Y < minY.Y) minY = point;
            if (point.Y > maxY.Y) maxY = point;
            if (point.Z < minZ.Z) minZ = point;
            if (point.Z > maxZ.Z) maxZ = point;
        }

        float spreadX = Vector3.DistanceSquared(maxX, minX);
        float spreadY = Vector3.DistanceSquared(maxY, minY);
        float spreadZ = Vector3.DistanceSquared(maxZ, minZ);

        Vector3 min = minX, max = maxX;
        if (spreadY > spreadX && spreadY > spreadZ)
        {
            min = minY;
            max = maxY;
        }

        if (spreadZ > spreadX && spreadZ > spreadY)
        {
            min = minZ;
            max = maxZ;
        }

        Vector3 center = (min + max) * 0.5f;
        float radius = Vector3.Distance(max, center);
        float squaredRadius = radius * radius;

        foreach (Vector3 point in all)
        {
            Vector3 offset = point - center;
            float squaredDistance = offset.LengthSquared();
            if (squaredDistance <= squaredRadius)
            {
                continue;
            }

            float distance = MathF.Sqrt(squaredDistance);
            Vector3 direction = offset / distance;

            // Grow just enough to reach `point` while keeping the far side where it was, rather
            // than re-centring on the mean -- that is what keeps every earlier point enclosed.
            Vector3 far = center - (radius * direction);
            center = (far + point) / 2f;
            radius = Vector3.Distance(point, center);
            squaredRadius = radius * radius;
        }

        return new BoundingSphere(center, radius);
    }

    /// <summary>The sphere around a frustum's eight corners.</summary>
    public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);

        return CreateFromPoints(frustum.GetCorners());
    }

    /// <summary>
    /// This sphere moved and scaled by <paramref name="matrix"/>.
    ///
    /// The radius scales by the largest of the three basis-row lengths, not by the matrix
    /// determinant or by an average: a non-uniform scale has to grow the sphere enough to still
    /// enclose everything it did before, which means the worst axis wins.
    /// </summary>
    public readonly BoundingSphere Transform(Matrix matrix)
    {
        float rowX = (matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12) + (matrix.M13 * matrix.M13);
        float rowY = (matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22) + (matrix.M23 * matrix.M23);
        float rowZ = (matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32) + (matrix.M33 * matrix.M33);

        return new BoundingSphere(
            Vector3.Transform(Center, matrix),
            Radius * MathF.Sqrt(MathF.Max(rowX, MathF.Max(rowY, rowZ))));
    }

    public static bool operator ==(BoundingSphere a, BoundingSphere b) => a.Equals(b);
    public static bool operator !=(BoundingSphere a, BoundingSphere b) => !a.Equals(b);

    public readonly bool Equals(BoundingSphere other) => Center.Equals(other.Center) && Radius.Equals(other.Radius);
    public override readonly bool Equals(object? obj) => obj is BoundingSphere other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Center, Radius);
    public override readonly string ToString() => $"{{Center:{Center} Radius:{Radius}}}";
}
