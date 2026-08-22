namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.PlaneConverter))]
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

    public Plane(Vector4 value)
    {
        Normal = new Vector3(value.X, value.Y, value.Z);
        D = value.W;
    }

    public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        Normal = Vector3.Normalize(Vector3.Cross(point2 - point1, point3 - point1));
        D = -Vector3.Dot(Normal, point1);
    }

    public readonly float DotCoordinate(Vector3 value) =>
        (Normal.X * value.X) + (Normal.Y * value.Y) + (Normal.Z * value.Z) + D;

    public readonly float Dot(Vector4 value) =>
        (Normal.X * value.X) + (Normal.Y * value.Y) + (Normal.Z * value.Z) + (D * value.W);

    public readonly void Dot(ref Vector4 value, out float result) => result = Dot(value);

    public readonly void DotCoordinate(ref Vector3 value, out float result) => result = DotCoordinate(value);

    public readonly float DotNormal(Vector3 value) =>
        (Normal.X * value.X) + (Normal.Y * value.Y) + (Normal.Z * value.Z);

    public readonly void DotNormal(ref Vector3 value, out float result) => result = DotNormal(value);

    public void Normalize()
    {
        float lengthSquared = (Normal.X * Normal.X) + (Normal.Y * Normal.Y) + (Normal.Z * Normal.Z);
        if (!(Math.Abs(lengthSquared - 1f) < 1.1920929E-07f))
        {
            float factor = 1f / (float)Math.Sqrt(lengthSquared);
            Normal.X *= factor;
            Normal.Y *= factor;
            Normal.Z *= factor;
            D *= factor;
        }
    }

    public static Plane Normalize(Plane value)
    {
        value.Normalize();
        return value;
    }

    public static void Normalize(ref Plane value, out Plane result) => result = Normalize(value);

    public readonly PlaneIntersectionType Intersects(BoundingSphere sphere)
    {
        float distance = (sphere.Center.X * Normal.X) +
            (sphere.Center.Y * Normal.Y) +
            (sphere.Center.Z * Normal.Z) + D;
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
        var negativeVertex = new Vector3(
            Normal.X >= 0f ? box.Min.X : box.Max.X,
            Normal.Y >= 0f ? box.Min.Y : box.Max.Y,
            Normal.Z >= 0f ? box.Min.Z : box.Max.Z);
        var positiveVertex = new Vector3(
            Normal.X >= 0f ? box.Max.X : box.Min.X,
            Normal.Y >= 0f ? box.Max.Y : box.Min.Y,
            Normal.Z >= 0f ? box.Max.Z : box.Min.Z);

        float distance = (Normal.X * negativeVertex.X) +
            (Normal.Y * negativeVertex.Y) +
            (Normal.Z * negativeVertex.Z) + D;
        if (distance > 0f)
        {
            return PlaneIntersectionType.Front;
        }

        distance = (Normal.X * positiveVertex.X) +
            (Normal.Y * positiveVertex.Y) +
            (Normal.Z * positiveVertex.Z) + D;
        return distance < 0f ? PlaneIntersectionType.Back : PlaneIntersectionType.Intersecting;
    }

    public readonly PlaneIntersectionType Intersects(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return frustum.Intersects(this);
    }

    public readonly void Intersects(ref BoundingBox box, out PlaneIntersectionType result) =>
        result = Intersects(box);

    public readonly void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result) =>
        result = Intersects(sphere);

    public static Plane Transform(Plane plane, Matrix matrix)
    {
        Transform(ref plane, ref matrix, out Plane result);
        return result;
    }

    public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
    {
        Matrix inverse = Matrix.Invert(matrix);
        float x = plane.Normal.X;
        float y = plane.Normal.Y;
        float z = plane.Normal.Z;
        float d = plane.D;
        result = new Plane(
            (x * inverse.M11) + (y * inverse.M12) + (z * inverse.M13) + (d * inverse.M14),
            (x * inverse.M21) + (y * inverse.M22) + (z * inverse.M23) + (d * inverse.M24),
            (x * inverse.M31) + (y * inverse.M32) + (z * inverse.M33) + (d * inverse.M34),
            (x * inverse.M41) + (y * inverse.M42) + (z * inverse.M43) + (d * inverse.M44));
    }

    public static Plane Transform(Plane plane, Quaternion rotation)
    {
        Transform(ref plane, ref rotation, out Plane result);
        return result;
    }

    public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
    {
        Vector3 normal = plane.Normal;
        Vector3.Transform(ref normal, ref rotation, out normal);
        result = new Plane(normal, plane.D);
    }

    public static bool operator ==(Plane lhs, Plane rhs) => lhs.Equals(rhs);
    public static bool operator !=(Plane lhs, Plane rhs) => !lhs.Equals(rhs);

    public readonly bool Equals(Plane other) => Normal == other.Normal && D == other.D;
    public override readonly bool Equals(object? obj) => obj is Plane other && Equals(other);
    public override readonly int GetHashCode() => Normal.GetHashCode() + D.GetHashCode();
    public override readonly string ToString() => $"{{Normal:{Normal} D:{D}}}";

    internal readonly CNA.Plane ToFramework() => new(Normal.ToFramework(), D);

    internal static Plane FromFramework(CNA.Plane value) => new(Vector3.FromFramework(value.Normal), value.D);
}
