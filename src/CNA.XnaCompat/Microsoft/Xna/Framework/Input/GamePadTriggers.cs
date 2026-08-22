namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadTriggers
{
    public float Left { get; }
    public float Right { get; }

    public GamePadTriggers(float leftTrigger, float rightTrigger)
    {
        Left = Math.Max(Math.Min(leftTrigger, 1f), 0f);
        Right = Math.Max(Math.Min(rightTrigger, 1f), 0f);
    }

    internal GamePadTriggers(CNA.Input.GamePadTriggers framework)
    {
        Left = framework.Left;
        Right = framework.Right;
    }

    public override bool Equals(object? obj) => obj is GamePadTriggers other && this == other;

    public override int GetHashCode() => XnaInputHash.Smart(
        XnaInputHash.FloatBits(Left), XnaInputHash.FloatBits(Right));

    public override string ToString() => $"{{Left:{Left} Right:{Right}}}";

    public static bool operator ==(GamePadTriggers left, GamePadTriggers right) =>
        left.Left == right.Left && left.Right == right.Right;

    public static bool operator !=(GamePadTriggers left, GamePadTriggers right) => !(left == right);
}
