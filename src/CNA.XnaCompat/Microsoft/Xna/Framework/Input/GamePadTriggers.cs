namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadTriggers
{
    public float Left { get; }
    public float Right { get; }

    internal GamePadTriggers(CNA.Input.GamePadTriggers framework)
    {
        Left = framework.Left;
        Right = framework.Right;
    }
}
