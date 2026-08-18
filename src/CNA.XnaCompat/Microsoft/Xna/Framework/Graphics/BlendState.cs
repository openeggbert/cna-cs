namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>BlendState</c>. A pure subclass -- every property is inherited
/// unchanged from <see cref="CNA.Graphics.BlendState"/>, same as <see cref="IndexBuffer"/>'s
/// <c>SetData</c>/<c>GetData</c>. Only the four static presets need their own compat-typed
/// versions, built through the base class's own copy constructor rather than duplicating its
/// native preset-init calls.</summary>
public class BlendState : CNA.Graphics.BlendState
{
    public BlendState()
    {
    }

    internal BlendState(CNA.Graphics.BlendState copyFrom)
        : base(copyFrom)
    {
    }

    public static new BlendState Opaque { get; } = new(CNA.Graphics.BlendState.Opaque);
    public static new BlendState AlphaBlend { get; } = new(CNA.Graphics.BlendState.AlphaBlend);
    public static new BlendState Additive { get; } = new(CNA.Graphics.BlendState.Additive);
    public static new BlendState NonPremultiplied { get; } = new(CNA.Graphics.BlendState.NonPremultiplied);
}
