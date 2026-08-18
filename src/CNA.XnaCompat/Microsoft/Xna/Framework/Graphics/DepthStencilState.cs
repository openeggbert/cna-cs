namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DepthStencilState</c>. See <see cref="BlendState"/>'s own doc
/// comment for the subclass/copy-constructor pattern.</summary>
public class DepthStencilState : CNA.Graphics.DepthStencilState
{
    public DepthStencilState()
    {
    }

    internal DepthStencilState(CNA.Graphics.DepthStencilState copyFrom)
        : base(copyFrom)
    {
    }

    public static new DepthStencilState Default { get; } = new(CNA.Graphics.DepthStencilState.Default);
    public static new DepthStencilState DepthRead { get; } = new(CNA.Graphics.DepthStencilState.DepthRead);
    public static new DepthStencilState None { get; } = new(CNA.Graphics.DepthStencilState.None);
}
