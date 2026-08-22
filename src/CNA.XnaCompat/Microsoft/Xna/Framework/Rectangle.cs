namespace Microsoft.Xna.Framework;

[System.ComponentModel.TypeConverter(typeof(Design.RectangleConverter))]
public struct Rectangle : IEquatable<Rectangle>
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public readonly bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;

    public static Rectangle Empty => new(0, 0, 0, 0);

    public readonly int Left => X;
    public readonly int Right => X + Width;
    public readonly int Top => Y;
    public readonly int Bottom => Y + Height;

    public Point Location
    {
        readonly get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public readonly Point Center => new(X + (Width / 2), Y + (Height / 2));

    public readonly bool Contains(int x, int y) => X <= x && x < Right && Y <= y && y < Bottom;

    public readonly bool Contains(Point value) => Contains(value.X, value.Y);

    public readonly void Contains(ref Point value, out bool result) => result = Contains(value);

    public readonly bool Contains(Rectangle value) => ToFramework().Contains(value.ToFramework());

    public readonly void Contains(ref Rectangle value, out bool result) => result = Contains(value);

    public readonly bool Intersects(Rectangle value) => ToFramework().Intersects(value.ToFramework());

    public readonly void Intersects(ref Rectangle value, out bool result) => result = Intersects(value);

    public static Rectangle Intersect(Rectangle value1, Rectangle value2) =>
        FromFramework(CNA.Rectangle.Intersect(value1.ToFramework(), value2.ToFramework()));

    public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result) =>
        result = Intersect(value1, value2);

    public static Rectangle Union(Rectangle value1, Rectangle value2) =>
        FromFramework(CNA.Rectangle.Union(value1.ToFramework(), value2.ToFramework()));

    public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result) =>
        result = Union(value1, value2);

    public void Inflate(int horizontalAmount, int verticalAmount)
    {
        X -= horizontalAmount;
        Y -= verticalAmount;
        Width += horizontalAmount * 2;
        Height += verticalAmount * 2;
    }

    public void Offset(int offsetX, int offsetY)
    {
        X += offsetX;
        Y += offsetY;
    }

    public void Offset(Point amount) => Offset(amount.X, amount.Y);

    public static bool operator ==(Rectangle a, Rectangle b) => a.Equals(b);
    public static bool operator !=(Rectangle a, Rectangle b) => !a.Equals(b);

    public readonly bool Equals(Rectangle other) =>
        X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    public override readonly bool Equals(object? obj) => obj is Rectangle other && Equals(other);
    public override readonly int GetHashCode() =>
        X.GetHashCode() + Y.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";

    internal readonly CNA.Rectangle ToFramework() => new(X, Y, Width, Height);

    internal static Rectangle FromFramework(CNA.Rectangle value) =>
        new(value.X, value.Y, value.Width, value.Height);
}
