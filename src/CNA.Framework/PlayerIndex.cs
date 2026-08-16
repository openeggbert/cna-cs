namespace CNA;

/// <summary>
/// Root namespace, not <c>CNA.Input</c>, matching real XNA: <c>PlayerIndex</c> is shared by
/// <see cref="Input.GamePad"/> and the (not yet implemented) GamerServices/Storage APIs, so
/// Microsoft put it at the top level rather than tying it to Input.
/// </summary>
public enum PlayerIndex
{
    One = 0,
    Two = 1,
    Three = 2,
    Four = 3,
}
