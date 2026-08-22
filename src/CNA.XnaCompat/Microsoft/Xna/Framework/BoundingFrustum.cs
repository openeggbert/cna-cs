using System.ComponentModel;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class BoundingFrustum : IEquatable<BoundingFrustum>
{
    public const int CornerCount = 8;

    private Matrix _matrix;
    private readonly Plane[] _planes = new Plane[6];
    internal readonly Vector3[] CornerArray = new Vector3[CornerCount];
    private Gjk? _gjk;

    public BoundingFrustum(Matrix value)
    {
        SetMatrix(ref value);
    }

    public Plane Near => _planes[0];

    public Plane Far => _planes[1];

    public Plane Left => _planes[2];

    public Plane Right => _planes[3];

    public Plane Top => _planes[4];

    public Plane Bottom => _planes[5];

    public Matrix Matrix
    {
        get => _matrix;
        set => SetMatrix(ref value);
    }

    public Vector3[] GetCorners() => (Vector3[])CornerArray.Clone();

    public void GetCorners(Vector3[] corners)
    {
        ArgumentNullException.ThrowIfNull(corners);
        if (corners.Length < CornerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(corners));
        }

        CornerArray.CopyTo(corners, 0);
    }

    public ContainmentType Contains(Vector3 point)
    {
        foreach (Plane plane in _planes)
        {
            float distance =
                (plane.Normal.X * point.X) +
                (plane.Normal.Y * point.Y) +
                (plane.Normal.Z * point.Z) +
                plane.D;
            if (distance > 1e-5f)
            {
                return ContainmentType.Disjoint;
            }
        }

        return ContainmentType.Contains;
    }

    public void Contains(ref Vector3 point, out ContainmentType result) => result = Contains(point);

    public ContainmentType Contains(BoundingBox box)
    {
        bool intersects = false;
        foreach (Plane plane in _planes)
        {
            switch (box.Intersects(plane))
            {
                case PlaneIntersectionType.Front:
                    return ContainmentType.Disjoint;
                case PlaneIntersectionType.Intersecting:
                    intersects = true;
                    break;
            }
        }

        return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
    }

    public void Contains(ref BoundingBox box, out ContainmentType result) => result = Contains(box);

    public ContainmentType Contains(BoundingSphere sphere)
    {
        int insidePlaneCount = 0;
        foreach (Plane plane in _planes)
        {
            float dot =
                (plane.Normal.X * sphere.Center.X) +
                (plane.Normal.Y * sphere.Center.Y) +
                (plane.Normal.Z * sphere.Center.Z);
            float distance = dot + plane.D;
            if (distance > sphere.Radius)
            {
                return ContainmentType.Disjoint;
            }

            if (distance < -sphere.Radius)
            {
                insidePlaneCount++;
            }
        }

        return insidePlaneCount == 6 ? ContainmentType.Contains : ContainmentType.Intersects;
    }

    public void Contains(ref BoundingSphere sphere, out ContainmentType result) => result = Contains(sphere);

    public ContainmentType Contains(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        if (!Intersects(frustum))
        {
            return ContainmentType.Disjoint;
        }

        foreach (Vector3 corner in frustum.CornerArray)
        {
            if (Contains(corner) == ContainmentType.Disjoint)
            {
                return ContainmentType.Intersects;
            }
        }

        return ContainmentType.Contains;
    }

    public bool Intersects(BoundingBox box)
    {
        _gjk ??= new Gjk();
        _gjk.Reset();

        Vector3 closestPoint = CornerArray[0] - box.Min;
        if (closestPoint.LengthSquared() < 1e-5f)
        {
            closestPoint = CornerArray[0] - box.Max;
        }

        float previousDistanceSquared = float.MaxValue;
        float threshold = 0f;
        Vector3 direction = default;
        do
        {
            direction.X = -closestPoint.X;
            direction.Y = -closestPoint.Y;
            direction.Z = -closestPoint.Z;
            SupportMapping(ref direction, out Vector3 frustumPoint);
            box.SupportMapping(ref closestPoint, out Vector3 boxPoint);
            Vector3 supportPoint = frustumPoint - boxPoint;
            float dot =
                (closestPoint.X * supportPoint.X) +
                (closestPoint.Y * supportPoint.Y) +
                (closestPoint.Z * supportPoint.Z);
            if (dot > 0f)
            {
                return false;
            }

            _gjk.AddSupportPoint(ref supportPoint);
            closestPoint = _gjk.ClosestPoint;
            float oldDistanceSquared = previousDistanceSquared;
            previousDistanceSquared = closestPoint.LengthSquared();
            if (oldDistanceSquared - previousDistanceSquared <= 1e-5f * oldDistanceSquared)
            {
                return false;
            }

            threshold = 4e-5f * _gjk.MaxLengthSquared;
        }
        while (!_gjk.FullSimplex && previousDistanceSquared >= threshold);

        return true;
    }

    public void Intersects(ref BoundingBox box, out bool result) => result = Intersects(box);

    public bool Intersects(BoundingSphere sphere)
    {
        _gjk ??= new Gjk();
        _gjk.Reset();

        Vector3 closestPoint = CornerArray[0] - sphere.Center;
        if (closestPoint.LengthSquared() < 1e-5f)
        {
            closestPoint = Vector3.UnitX;
        }

        float previousDistanceSquared = float.MaxValue;
        float threshold = 0f;
        Vector3 direction = default;
        do
        {
            direction.X = -closestPoint.X;
            direction.Y = -closestPoint.Y;
            direction.Z = -closestPoint.Z;
            SupportMapping(ref direction, out Vector3 frustumPoint);
            sphere.SupportMapping(ref closestPoint, out Vector3 spherePoint);
            Vector3 supportPoint = frustumPoint - spherePoint;
            float dot =
                (closestPoint.X * supportPoint.X) +
                (closestPoint.Y * supportPoint.Y) +
                (closestPoint.Z * supportPoint.Z);
            if (dot > 0f)
            {
                return false;
            }

            _gjk.AddSupportPoint(ref supportPoint);
            closestPoint = _gjk.ClosestPoint;
            float oldDistanceSquared = previousDistanceSquared;
            previousDistanceSquared = closestPoint.LengthSquared();
            if (oldDistanceSquared - previousDistanceSquared <= 1e-5f * oldDistanceSquared)
            {
                return false;
            }

            threshold = 4e-5f * _gjk.MaxLengthSquared;
        }
        while (!_gjk.FullSimplex && previousDistanceSquared >= threshold);

        return true;
    }

    public void Intersects(ref BoundingSphere sphere, out bool result) => result = Intersects(sphere);

    public bool Intersects(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _gjk ??= new Gjk();
        _gjk.Reset();

        Vector3 closestPoint = CornerArray[0] - frustum.CornerArray[0];
        if (closestPoint.LengthSquared() < 1e-5f)
        {
            closestPoint = CornerArray[0] - frustum.CornerArray[1];
        }

        float previousDistanceSquared = float.MaxValue;
        float threshold = 0f;
        Vector3 direction = default;
        do
        {
            direction.X = -closestPoint.X;
            direction.Y = -closestPoint.Y;
            direction.Z = -closestPoint.Z;
            SupportMapping(ref direction, out Vector3 point1);
            frustum.SupportMapping(ref closestPoint, out Vector3 point2);
            Vector3 supportPoint = point1 - point2;
            float dot =
                (closestPoint.X * supportPoint.X) +
                (closestPoint.Y * supportPoint.Y) +
                (closestPoint.Z * supportPoint.Z);
            if (dot > 0f)
            {
                return false;
            }

            _gjk.AddSupportPoint(ref supportPoint);
            closestPoint = _gjk.ClosestPoint;
            float oldDistanceSquared = previousDistanceSquared;
            previousDistanceSquared = closestPoint.LengthSquared();
            threshold = 4e-5f * _gjk.MaxLengthSquared;
            if (oldDistanceSquared - previousDistanceSquared <= 1e-5f * oldDistanceSquared)
            {
                return false;
            }
        }
        while (!_gjk.FullSimplex && previousDistanceSquared >= threshold);

        return true;
    }

    public PlaneIntersectionType Intersects(Plane plane)
    {
        int sideMask = 0;
        foreach (Vector3 corner in CornerArray)
        {
            float dot = Vector3.Dot(corner, plane.Normal);
            sideMask = dot + plane.D > 0f ? sideMask | 1 : sideMask | 2;
            if (sideMask == 3)
            {
                return PlaneIntersectionType.Intersecting;
            }
        }

        return sideMask == 1 ? PlaneIntersectionType.Front : PlaneIntersectionType.Back;
    }

    public void Intersects(ref Plane plane, out PlaneIntersectionType result) => result = Intersects(plane);

    public float? Intersects(Ray ray)
    {
        Contains(ref ray.Position, out ContainmentType containment);
        if (containment == ContainmentType.Contains)
        {
            return 0f;
        }

        float entry = float.MinValue;
        float exit = float.MaxValue;
        foreach (Plane plane in _planes)
        {
            Vector3 normal = plane.Normal;
            float directionDot = Vector3.Dot(ray.Direction, normal);
            float positionDot = Vector3.Dot(ray.Position, normal) + plane.D;
            if (Math.Abs(directionDot) < 1e-5f)
            {
                if (positionDot > 0f)
                {
                    return null;
                }

                continue;
            }

            float distance = -positionDot / directionDot;
            if (directionDot < 0f)
            {
                if (distance > exit)
                {
                    return null;
                }

                if (distance > entry)
                {
                    entry = distance;
                }
            }
            else
            {
                if (distance < entry)
                {
                    return null;
                }

                if (distance < exit)
                {
                    exit = distance;
                }
            }
        }

        float result = entry >= 0f ? entry : exit;
        return result >= 0f ? result : null;
    }

    public void Intersects(ref Ray ray, out float? result) => result = Intersects(ray);

    public bool Equals(BoundingFrustum? other) => other is not null && _matrix == other._matrix;

    public override bool Equals(object? obj) => obj is BoundingFrustum other && _matrix == other._matrix;

    public override int GetHashCode() => _matrix.GetHashCode();

    public override string ToString() =>
        $"{{Near:{Near} Far:{Far} Left:{Left} Right:{Right} Top:{Top} Bottom:{Bottom}}}";

    public static bool operator ==(BoundingFrustum? a, BoundingFrustum? b) => Equals(a, b);

    public static bool operator !=(BoundingFrustum? a, BoundingFrustum? b) => !Equals(a, b);

    internal void SupportMapping(ref Vector3 direction, out Vector3 result)
    {
        int selectedIndex = 0;
        float selectedDot = Vector3.Dot(CornerArray[0], direction);
        for (int i = 1; i < CornerArray.Length; i++)
        {
            float dot = Vector3.Dot(CornerArray[i], direction);
            if (dot > selectedDot)
            {
                selectedIndex = i;
                selectedDot = dot;
            }
        }

        result = CornerArray[selectedIndex];
    }

    private void SetMatrix(ref Matrix value)
    {
        _matrix = value;

        _planes[2].Normal.X = -value.M14 - value.M11;
        _planes[2].Normal.Y = -value.M24 - value.M21;
        _planes[2].Normal.Z = -value.M34 - value.M31;
        _planes[2].D = -value.M44 - value.M41;
        _planes[3].Normal.X = -value.M14 + value.M11;
        _planes[3].Normal.Y = -value.M24 + value.M21;
        _planes[3].Normal.Z = -value.M34 + value.M31;
        _planes[3].D = -value.M44 + value.M41;
        _planes[4].Normal.X = -value.M14 + value.M12;
        _planes[4].Normal.Y = -value.M24 + value.M22;
        _planes[4].Normal.Z = -value.M34 + value.M32;
        _planes[4].D = -value.M44 + value.M42;
        _planes[5].Normal.X = -value.M14 - value.M12;
        _planes[5].Normal.Y = -value.M24 - value.M22;
        _planes[5].Normal.Z = -value.M34 - value.M32;
        _planes[5].D = -value.M44 - value.M42;
        _planes[0].Normal.X = -value.M13;
        _planes[0].Normal.Y = -value.M23;
        _planes[0].Normal.Z = -value.M33;
        _planes[0].D = -value.M43;
        _planes[1].Normal.X = -value.M14 + value.M13;
        _planes[1].Normal.Y = -value.M24 + value.M23;
        _planes[1].Normal.Z = -value.M34 + value.M33;
        _planes[1].D = -value.M44 + value.M43;

        for (int i = 0; i < _planes.Length; i++)
        {
            float length = _planes[i].Normal.Length();
            _planes[i].Normal /= length;
            _planes[i].D /= length;
        }

        Ray ray = ComputeIntersectionLine(ref _planes[0], ref _planes[2]);
        CornerArray[0] = ComputeIntersection(ref _planes[4], ref ray);
        CornerArray[3] = ComputeIntersection(ref _planes[5], ref ray);
        ray = ComputeIntersectionLine(ref _planes[3], ref _planes[0]);
        CornerArray[1] = ComputeIntersection(ref _planes[4], ref ray);
        CornerArray[2] = ComputeIntersection(ref _planes[5], ref ray);
        ray = ComputeIntersectionLine(ref _planes[2], ref _planes[1]);
        CornerArray[4] = ComputeIntersection(ref _planes[4], ref ray);
        CornerArray[7] = ComputeIntersection(ref _planes[5], ref ray);
        ray = ComputeIntersectionLine(ref _planes[1], ref _planes[3]);
        CornerArray[5] = ComputeIntersection(ref _planes[4], ref ray);
        CornerArray[6] = ComputeIntersection(ref _planes[5], ref ray);
    }

    private static Ray ComputeIntersectionLine(ref Plane plane1, ref Plane plane2)
    {
        Ray result = new() { Direction = Vector3.Cross(plane1.Normal, plane2.Normal) };
        float lengthSquared = result.Direction.LengthSquared();
        result.Position = Vector3.Cross(
            (-plane1.D * plane2.Normal) + (plane2.D * plane1.Normal),
            result.Direction) / lengthSquared;
        return result;
    }

    private static Vector3 ComputeIntersection(ref Plane plane, ref Ray ray)
    {
        float distance =
            (-plane.D - Vector3.Dot(plane.Normal, ray.Position)) /
            Vector3.Dot(plane.Normal, ray.Direction);
        return ray.Position + (ray.Direction * distance);
    }
}
