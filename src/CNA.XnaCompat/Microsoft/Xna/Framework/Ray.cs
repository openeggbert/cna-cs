namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.RayConverter))]
public struct Ray : IEquatable<Ray>
{
    public Vector3 Position;
    public Vector3 Direction;

    public Ray(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction;
    }

    public readonly float? Intersects(BoundingBox box) => box.Intersects(this);

    public readonly void Intersects(ref BoundingBox box, out float? result) => result = Intersects(box);

    public readonly float? Intersects(BoundingSphere sphere)
    {
        float x = sphere.Center.X - Position.X;
        float y = sphere.Center.Y - Position.Y;
        float z = sphere.Center.Z - Position.Z;
        float distanceSquared = (x * x) + (y * y) + (z * z);
        float radiusSquared = sphere.Radius * sphere.Radius;
        if (distanceSquared <= radiusSquared)
        {
            return 0f;
        }

        float projection = (x * Direction.X) + (y * Direction.Y) + (z * Direction.Z);
        if (projection < 0f)
        {
            return null;
        }

        float closestDistanceSquared = distanceSquared - (projection * projection);
        if (closestDistanceSquared > radiusSquared)
        {
            return null;
        }

        float halfChord = (float)Math.Sqrt(radiusSquared - closestDistanceSquared);
        return projection - halfChord;
    }

    public readonly void Intersects(ref BoundingSphere sphere, out float? result) => result = Intersects(sphere);

    public readonly float? Intersects(Plane plane)
    {
        const float epsilon = 1e-5f;
        float denominator =
            (plane.Normal.X * Direction.X) +
            (plane.Normal.Y * Direction.Y) +
            (plane.Normal.Z * Direction.Z);
        if (Math.Abs(denominator) < epsilon)
        {
            return null;
        }

        float positionDot =
            (plane.Normal.X * Position.X) +
            (plane.Normal.Y * Position.Y) +
            (plane.Normal.Z * Position.Z);
        float distance = (-plane.D - positionDot) / denominator;
        if (distance < 0f)
        {
            return distance < -epsilon ? null : 0f;
        }

        return distance;
    }

    public readonly void Intersects(ref Plane plane, out float? result)
    {
        const float epsilon = 1e-5f;
        float denominator =
            (plane.Normal.X * Direction.X) +
            (plane.Normal.Y * Direction.Y) +
            (plane.Normal.Z * Direction.Z);
        if (Math.Abs(denominator) < epsilon)
        {
            result = null;
            return;
        }

        float positionDot =
            (plane.Normal.X * Position.X) +
            (plane.Normal.Y * Position.Y) +
            (plane.Normal.Z * Position.Z);
        float distance = (-plane.D - positionDot) / denominator;
        if (distance < 0f)
        {
            if (distance < -epsilon)
            {
                result = null;
                return;
            }

            distance = 0f;
        }

        result = distance;
    }

    public readonly float? Intersects(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return frustum.Intersects(this);
    }

    public static bool operator ==(Ray a, Ray b) => a.Equals(b);
    public static bool operator !=(Ray a, Ray b) => !a.Equals(b);

    public readonly bool Equals(Ray other) => Position == other.Position && Direction == other.Direction;
    public override readonly bool Equals(object? obj) => obj is Ray other && Equals(other);
    public override readonly int GetHashCode() => Position.GetHashCode() + Direction.GetHashCode();
    public override readonly string ToString() => $"{{Position:{Position} Direction:{Direction}}}";

    internal readonly CNA.Ray ToFramework() => new(Position.ToFramework(), Direction.ToFramework());

    internal static Ray FromFramework(CNA.Ray value) =>
        new(Vector3.FromFramework(value.Position), Vector3.FromFramework(value.Direction));
}
