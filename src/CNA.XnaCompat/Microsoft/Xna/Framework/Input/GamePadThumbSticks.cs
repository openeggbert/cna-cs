namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadThumbSticks
{
    public Vector2 Left { get; }
    public Vector2 Right { get; }

    public GamePadThumbSticks(Vector2 leftThumbstick, Vector2 rightThumbstick)
    {
        Left = Vector2.Max(Vector2.Min(leftThumbstick, Vector2.One), -Vector2.One);
        Right = Vector2.Max(Vector2.Min(rightThumbstick, Vector2.One), -Vector2.One);
    }

    internal GamePadThumbSticks(CNA.Input.GamePadThumbSticks framework)
    {
        Left = framework.Left.ToCompat();
        Right = framework.Right.ToCompat();
    }

    public override bool Equals(object? obj) => obj is GamePadThumbSticks other &&
        Left == other.Left && Right == other.Right;

    public override int GetHashCode() => XnaInputHash.Smart(
        XnaInputHash.FloatBits(Left.X),
        XnaInputHash.FloatBits(Left.Y),
        XnaInputHash.FloatBits(Right.X),
        XnaInputHash.FloatBits(Right.Y));

    public override string ToString() => $"{{Left:{Left} Right:{Right}}}";

    public static bool operator ==(GamePadThumbSticks left, GamePadThumbSticks right) => left.Equals(right);

    public static bool operator !=(GamePadThumbSticks left, GamePadThumbSticks right) => !left.Equals(right);
}
