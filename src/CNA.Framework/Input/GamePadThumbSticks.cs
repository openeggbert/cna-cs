namespace CNA.Input;

public readonly struct GamePadThumbSticks
{
    public Vector2 Left { get; }
    public Vector2 Right { get; }

    public GamePadThumbSticks(Vector2 left, Vector2 right)
    {
        Left = left;
        Right = right;
    }
}
