namespace Microsoft.Xna.Framework;

public struct Point : IEquatable<Point>
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point Zero => new(0, 0);

    public static bool operator ==(Point a, Point b) => a.Equals(b);
    public static bool operator !=(Point a, Point b) => !a.Equals(b);

    public readonly bool Equals(Point other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is Point other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public override readonly string ToString() => $"{{X:{X} Y:{Y}}}";

    public static implicit operator CNA.Point(Point value) => new(value.X, value.Y);
    public static implicit operator Point(CNA.Point value) => new(value.X, value.Y);
}
