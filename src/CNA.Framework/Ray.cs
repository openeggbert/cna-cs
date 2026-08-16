namespace CNA;

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

    public readonly float? Intersects(BoundingSphere sphere) => sphere.Intersects(this);

    public readonly float? Intersects(BoundingFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        return frustum.Intersects(this);
    }

    public readonly float? Intersects(Plane plane)
    {
        float denominator = Vector3.Dot(plane.Normal, Direction);
        if (MathF.Abs(denominator) < float.Epsilon)
        {
            return null;
        }

        float t = -(Vector3.Dot(plane.Normal, Position) + plane.D) / denominator;
        return t < 0f ? null : t;
    }

    public static bool operator ==(Ray a, Ray b) => a.Equals(b);
    public static bool operator !=(Ray a, Ray b) => !a.Equals(b);

    public readonly bool Equals(Ray other) => Position.Equals(other.Position) && Direction.Equals(other.Direction);
    public override readonly bool Equals(object? obj) => obj is Ray other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, Direction);
    public override readonly string ToString() => $"{{Position:{Position} Direction:{Direction}}}";
}
