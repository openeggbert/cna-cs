namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DualTextureEffect</c>. Extends
/// <see cref="CNA.Graphics.DualTextureEffect"/> directly, exactly as <see cref="BasicEffect"/> does
/// -- see that type's own doc comment for the trade-off and for the identical, documented gap
/// (<c>CurrentTechnique</c>/<c>Passes</c>, and the directional lights where present, are inherited
/// unchanged and so report <c>CNA.Graphics</c>-namespaced types). Every property here involves only
/// <see cref="Vector3"/>/<see cref="Matrix"/>/<see cref="float"/>/<see cref="bool"/>, which convert
/// implicitly across the boundary, so nothing needs re-typing.</summary>
public class DualTextureEffect : CNA.Graphics.DualTextureEffect, IEffectMatrices, IEffectFog
{
    public DualTextureEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    Matrix IEffectMatrices.World
    {
        get => base.World;
        set => base.World = value;
    }

    Matrix IEffectMatrices.View
    {
        get => base.View;
        set => base.View = value;
    }

    Matrix IEffectMatrices.Projection
    {
        get => base.Projection;
        set => base.Projection = value;
    }

    Vector3 IEffectFog.FogColor
    {
        get => base.FogColor;
        set => base.FogColor = value;
    }

    bool IEffectFog.FogEnabled
    {
        get => base.FogEnabled;
        set => base.FogEnabled = value;
    }

    float IEffectFog.FogStart
    {
        get => base.FogStart;
        set => base.FogStart = value;
    }

    float IEffectFog.FogEnd
    {
        get => base.FogEnd;
        set => base.FogEnd = value;
    }

}
