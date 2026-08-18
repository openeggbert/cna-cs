namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>AlphaTestEffect</c>. Extends
/// <see cref="CNA.Graphics.AlphaTestEffect"/> directly, exactly as <see cref="BasicEffect"/> does
/// -- see that type's own doc comment for the trade-off and for the identical, documented gap
/// (<c>CurrentTechnique</c>/<c>Passes</c>, and the directional lights where present, are inherited
/// unchanged and so report <c>CNA.Graphics</c>-namespaced types). Every property here involves only
/// <see cref="Vector3"/>/<see cref="Matrix"/>/<see cref="float"/>/<see cref="bool"/>, which convert
/// implicitly across the boundary, except <see cref="AlphaFunction"/> below.</summary>
public class AlphaTestEffect : CNA.Graphics.AlphaTestEffect
{
    public AlphaTestEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    /// <summary>The one member that does need re-typing here: <see cref="CompareFunction"/> is
    /// duplicated per namespace (see that enum's own doc comment), unlike the vector/matrix/scalar
    /// properties this class inherits unchanged.</summary>
    public new CompareFunction AlphaFunction
    {
        get => (CompareFunction)(int)base.AlphaFunction;
        set => base.AlphaFunction = (CNA.Graphics.CompareFunction)(int)value;
    }
}
