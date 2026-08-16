namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadThumbSticks
{
    public Vector2 Left { get; }
    public Vector2 Right { get; }

    internal GamePadThumbSticks(CNA.Input.GamePadThumbSticks framework)
    {
        Left = framework.Left;
        Right = framework.Right;
    }
}
