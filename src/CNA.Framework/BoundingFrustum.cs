namespace CNA;

/// <summary>
/// A view-projection frustum. Plane extraction uses the standard homogeneous-clip-space
/// derivation (a row-vector-convention adaptation of the Gribb-Hartmann method), cross-checked
/// against this project's own <see cref="Matrix.CreatePerspectiveFieldOfView"/> /
/// <see cref="Matrix.CreateLookAt"/> conventions for consistency. Corners are computed by
/// unprojecting the NDC cube through <see cref="Matrix.Invert(Matrix)"/> rather than by
/// intersecting planes, which keeps the two derivations independent. See BoundingFrustumTests
/// for the containment checks this is validated against.
/// </summary>
public class BoundingFrustum : IEquatable<BoundingFrustum>
{
    public const int CornerCount = 8;

    private Matrix _matrix;
    private readonly Plane[] _planes = new Plane[6];
    private readonly Vector3[] _corners = new Vector3[8];

    public BoundingFrustum(Matrix value)
    {
        Matrix = value;
    }

    public Matrix Matrix
    {
        get => _matrix;
        set
        {
            _matrix = value;
            ExtractPlanes();
            ExtractCorners();
        }
    }

    public Plane Near => _planes[0];
    public Plane Far => _planes[1];
    public Plane Left => _planes[2];
    public Plane Right => _planes[3];
    public Plane Bottom => _planes[4];
    public Plane Top => _planes[5];

    public Vector3[] GetCorners() => (Vector3[])_corners.Clone();

    public bool Contains(Vector3 point)
    {
        foreach (Plane plane in _planes)
        {
            if (plane.DotCoordinate(point) < 0f)
            {
                return false;
            }
        }

        return true;
    }

    public ContainmentType Contains(BoundingBox box) => ContainsCorners(box.GetCorners());

    /// <summary>
    /// Same corner-vs-plane test as <see cref="Contains(BoundingBox)"/>, applied to
    /// <paramref name="frustum"/>'s 8 corners instead of a box's. This is the same
    /// approximation real XNA/MonoGame's own <c>BoundingFrustum.Contains(BoundingFrustum)</c>
    /// uses -- matching real XNA's actual (imperfect) behavior is the goal here, not a
    /// mathematically exact convex-polytope separating-axis test. It can report
    /// <see cref="ContainmentType.Intersects"/> in the rare case where two frustums overlap with
    /// no corner of either inside the other (an edge/face crossing with no vertex containment) --
    /// a true SAT test would need the face normals of both frustums plus their pairwise edge
    /// cross products as separating axes, which real XNA does not do either.
    /// </summary>
    public ContainmentType Contains(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return ContainsCorners(frustum.GetCorners());
    }

    private ContainmentType ContainsCorners(Vector3[] corners)
    {
        bool intersecting = false;

        foreach (Plane plane in _planes)
        {
            int outsideCount = 0;
            foreach (Vector3 corner in corners)
            {
                if (plane.DotCoordinate(corner) < 0f)
                {
                    outsideCount++;
                }
            }

            if (outsideCount == corners.Length)
            {
                return ContainmentType.Disjoint;
            }

            if (outsideCount > 0)
            {
                intersecting = true;
            }
        }

        return intersecting ? ContainmentType.Intersects : ContainmentType.Contains;
    }

    public ContainmentType Contains(BoundingSphere sphere)
    {
        bool intersecting = false;

        foreach (Plane plane in _planes)
        {
            float distance = plane.DotCoordinate(sphere.Center);

            if (distance < -sphere.Radius)
            {
                return ContainmentType.Disjoint;
            }

            if (distance < sphere.Radius)
            {
                intersecting = true;
            }
        }

        return intersecting ? ContainmentType.Intersects : ContainmentType.Contains;
    }

    public bool Intersects(BoundingBox box) => Contains(box) != ContainmentType.Disjoint;

    public bool Intersects(BoundingSphere sphere) => Contains(sphere) != ContainmentType.Disjoint;

    public bool Intersects(BoundingFrustum frustum) => Contains(frustum) != ContainmentType.Disjoint;

    /// <summary>
    /// Standard "ray vs. intersection of half-spaces" slab test, generalized from the classic
    /// AABB slab test to the frustum's 6 arbitrary planes instead of 3 axis-aligned pairs. Not
    /// XNA-specific -- this is the textbook technique for ray-vs-convex-region intersection.
    /// Returns 0 (not <c>null</c>) when <paramref name="ray"/>'s origin already starts inside the
    /// frustum, matching <see cref="Ray.Intersects(BoundingBox)"/>'s existing "distance from
    /// origin" contract.
    /// </summary>
    public float? Intersects(Ray ray)
    {
        float tMin = 0f;
        float tMax = float.MaxValue;

        foreach (Plane plane in _planes)
        {
            float denominator = Vector3.Dot(plane.Normal, ray.Direction);
            float distance = plane.DotCoordinate(ray.Position);

            if (MathF.Abs(denominator) < 1e-6f)
            {
                if (distance < 0f)
                {
                    return null;
                }

                continue;
            }

            float t = -distance / denominator;

            if (denominator > 0f)
            {
                tMin = MathF.Max(tMin, t);
            }
            else
            {
                tMax = MathF.Min(tMax, t);
            }

            if (tMin > tMax)
            {
                return null;
            }
        }

        return tMin;
    }

    private void ExtractPlanes()
    {
        // Homogeneous clip-space half-space coefficients for row-vector convention (v * M),
        // matching this project's Matrix/Vector4 transform convention.
        _planes[0] = Plane.Normalize(new Plane(_matrix.M13, _matrix.M23, _matrix.M33, _matrix.M43)); // Near: clip.z >= 0
        _planes[1] = Plane.Normalize(new Plane(
            _matrix.M14 - _matrix.M13, _matrix.M24 - _matrix.M23, _matrix.M34 - _matrix.M33, _matrix.M44 - _matrix.M43)); // Far
        _planes[2] = Plane.Normalize(new Plane(
            _matrix.M11 + _matrix.M14, _matrix.M21 + _matrix.M24, _matrix.M31 + _matrix.M34, _matrix.M41 + _matrix.M44)); // Left
        _planes[3] = Plane.Normalize(new Plane(
            _matrix.M14 - _matrix.M11, _matrix.M24 - _matrix.M21, _matrix.M34 - _matrix.M31, _matrix.M44 - _matrix.M41)); // Right
        _planes[4] = Plane.Normalize(new Plane(
            _matrix.M12 + _matrix.M14, _matrix.M22 + _matrix.M24, _matrix.M32 + _matrix.M34, _matrix.M42 + _matrix.M44)); // Bottom
        _planes[5] = Plane.Normalize(new Plane(
            _matrix.M14 - _matrix.M12, _matrix.M24 - _matrix.M22, _matrix.M34 - _matrix.M32, _matrix.M44 - _matrix.M42)); // Top
    }

    private void ExtractCorners()
    {
        Matrix inverse = Matrix.Invert(_matrix);

        _corners[0] = Unproject(-1f, 1f, 0f, inverse);
        _corners[1] = Unproject(1f, 1f, 0f, inverse);
        _corners[2] = Unproject(1f, -1f, 0f, inverse);
        _corners[3] = Unproject(-1f, -1f, 0f, inverse);
        _corners[4] = Unproject(-1f, 1f, 1f, inverse);
        _corners[5] = Unproject(1f, 1f, 1f, inverse);
        _corners[6] = Unproject(1f, -1f, 1f, inverse);
        _corners[7] = Unproject(-1f, -1f, 1f, inverse);

        static Vector3 Unproject(float ndcX, float ndcY, float ndcZ, Matrix inverseViewProjection)
        {
            Vector4 clip = new(ndcX, ndcY, ndcZ, 1f);
            Vector4 world = Vector4.Transform(clip, inverseViewProjection);
            return world.W is > -float.Epsilon and < float.Epsilon
                ? new Vector3(world.X, world.Y, world.Z)
                : new Vector3(world.X / world.W, world.Y / world.W, world.Z / world.W);
        }
    }

    public static bool operator ==(BoundingFrustum? a, BoundingFrustum? b) => Equals(a, b);
    public static bool operator !=(BoundingFrustum? a, BoundingFrustum? b) => !Equals(a, b);

    public bool Equals(BoundingFrustum? other) => other is not null && _matrix.Equals(other._matrix);
    public override bool Equals(object? obj) => Equals(obj as BoundingFrustum);
    /// <summary>Matches real XNA's format: the six planes in Near/Far/Left/Right/Top/Bottom
    /// order.</summary>
    public override string ToString() =>
        $"{{Near:{Near} Far:{Far} Left:{Left} Right:{Right} Top:{Top} Bottom:{Bottom}}}";

    public override int GetHashCode() => _matrix.GetHashCode();
}
