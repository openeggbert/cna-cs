namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>RasterizerState</c>. See <see cref="BlendState"/>'s own doc
/// comment for the subclass/copy-constructor pattern.</summary>
public class RasterizerState : CNA.Graphics.RasterizerState
{
    public RasterizerState()
    {
    }

    internal RasterizerState(CNA.Graphics.RasterizerState copyFrom)
        : base(copyFrom)
    {
    }

    public static new RasterizerState CullClockwise { get; } = new(CNA.Graphics.RasterizerState.CullClockwise);
    public static new RasterizerState CullCounterClockwise { get; } = new(CNA.Graphics.RasterizerState.CullCounterClockwise);
    public static new RasterizerState CullNone { get; } = new(CNA.Graphics.RasterizerState.CullNone);
}
