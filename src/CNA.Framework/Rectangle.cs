namespace CNA;

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

    /// <summary>Matches real XNA's actual (surprising) definition: all four fields must be zero.</summary>
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

    public readonly bool Contains(Rectangle value) =>
        X <= value.X && value.Right <= Right && Y <= value.Y && value.Bottom <= Bottom;

    public readonly bool Intersects(Rectangle value) =>
        value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;

    public static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        int left = Math.Max(a.Left, b.Left);
        int top = Math.Max(a.Top, b.Top);
        int right = Math.Min(a.Right, b.Right);
        int bottom = Math.Min(a.Bottom, b.Bottom);
        return right > left && bottom > top ? new Rectangle(left, top, right - left, bottom - top) : Empty;
    }

    public static Rectangle Union(Rectangle a, Rectangle b)
    {
        int left = Math.Min(a.Left, b.Left);
        int top = Math.Min(a.Top, b.Top);
        int right = Math.Max(a.Right, b.Right);
        int bottom = Math.Max(a.Bottom, b.Bottom);
        return new Rectangle(left, top, right - left, bottom - top);
    }

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
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";

    internal readonly CNA.Interop.CnaRect ToNative() => new(X, Y, Width, Height);
}
