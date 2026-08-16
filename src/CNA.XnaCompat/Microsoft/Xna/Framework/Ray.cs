namespace Microsoft.Xna.Framework;

public struct Ray : IEquatable<Ray>
{
    public Vector3 Position;
    public Vector3 Direction;

    public Ray(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction;
    }

    public readonly float? Intersects(BoundingBox box) => ((CNA.Framework.Ray)this).Intersects(box);

    public readonly float? Intersects(BoundingSphere sphere) => ((CNA.Framework.Ray)this).Intersects(sphere);

    public readonly float? Intersects(Plane plane) => ((CNA.Framework.Ray)this).Intersects(plane);

    public static bool operator ==(Ray a, Ray b) => a.Equals(b);
    public static bool operator !=(Ray a, Ray b) => !a.Equals(b);

    public readonly bool Equals(Ray other) => Position.Equals(other.Position) && Direction.Equals(other.Direction);
    public override readonly bool Equals(object? obj) => obj is Ray other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, Direction);
    public override readonly string ToString() => $"{{Position:{Position} Direction:{Direction}}}";

    public static implicit operator CNA.Framework.Ray(Ray value) => new(value.Position, value.Direction);
    public static implicit operator Ray(CNA.Framework.Ray value) => new(value.Position, value.Direction);
}
