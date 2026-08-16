namespace CNA.Framework;

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
        Vector3 normal = Vector3.Normalize(Vector3.Cross(point2 - point1, point3 - point1));
        Normal = normal;
        D = -Vector3.Dot(normal, point1);
    }

    public readonly float DotCoordinate(Vector3 value) => Vector3.Dot(Normal, value) + D;

    public readonly float DotNormal(Vector3 value) => Vector3.Dot(Normal, value);

    public void Normalize()
    {
        float length = Normal.Length();
        if (length < float.Epsilon)
        {
            return;
        }

        float inverse = 1f / length;
        Normal *= inverse;
        D *= inverse;
    }

    public static Plane Normalize(Plane value)
    {
        value.Normalize();
        return value;
    }

    public readonly PlaneIntersectionType Intersects(BoundingSphere sphere)
    {
        float distance = DotCoordinate(sphere.Center);
        if (distance > sphere.Radius)
        {
            return PlaneIntersectionType.Front;
        }

        if (distance < -sphere.Radius)
        {
            return PlaneIntersectionType.Back;
        }

        return PlaneIntersectionType.Intersecting;
    }

    public readonly PlaneIntersectionType Intersects(BoundingBox box)
    {
        bool anyFront = false;
        bool anyBack = false;

        foreach (Vector3 corner in box.GetCorners())
        {
            if (DotCoordinate(corner) > 0f)
            {
                anyFront = true;
            }
            else
            {
                anyBack = true;
            }
        }

        if (anyFront && anyBack)
        {
            return PlaneIntersectionType.Intersecting;
        }

        return anyFront ? PlaneIntersectionType.Front : PlaneIntersectionType.Back;
    }

    public static bool operator ==(Plane a, Plane b) => a.Equals(b);
    public static bool operator !=(Plane a, Plane b) => !a.Equals(b);

    public readonly bool Equals(Plane other) => Normal.Equals(other.Normal) && D.Equals(other.D);
    public override readonly bool Equals(object? obj) => obj is Plane other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Normal, D);
    public override readonly string ToString() => $"{{Normal:{Normal} D:{D}}}";
}
