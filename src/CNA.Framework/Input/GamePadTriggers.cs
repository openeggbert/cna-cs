namespace CNA.Input;

public readonly struct GamePadTriggers
{
    public float Left { get; }
    public float Right { get; }

    public GamePadTriggers(float left, float right)
    {
        Left = left;
        Right = right;
    }
}
