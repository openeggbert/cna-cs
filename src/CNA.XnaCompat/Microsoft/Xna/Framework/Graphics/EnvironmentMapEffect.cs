namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EnvironmentMapEffect</c>. Extends
/// <see cref="CNA.Graphics.EnvironmentMapEffect"/> directly, exactly as <see cref="BasicEffect"/> does
/// -- see that type's own doc comment for the trade-off and for the identical, documented gap
/// (<c>CurrentTechnique</c>/<c>Passes</c>, and the directional lights where present, are inherited
/// unchanged and so report <c>CNA.Graphics</c>-namespaced types). Every property here involves only
/// <see cref="Vector3"/>/<see cref="Matrix"/>/<see cref="float"/>/<see cref="bool"/>, which convert
/// implicitly across the boundary, so nothing needs re-typing.</summary>
public class EnvironmentMapEffect : CNA.Graphics.EnvironmentMapEffect
{
    public EnvironmentMapEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }
}
