namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.PointConverter))]
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
    public override readonly int GetHashCode() => X.GetHashCode() + Y.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y}}}";

    internal readonly CNA.Point ToFramework() => new(X, Y);

    internal static Point FromFramework(CNA.Point value) => new(value.X, value.Y);
}
