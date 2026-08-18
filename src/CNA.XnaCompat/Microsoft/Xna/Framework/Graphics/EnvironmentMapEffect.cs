namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EnvironmentMapEffect</c>. Extends
/// <see cref="CNA.Graphics.EnvironmentMapEffect"/> directly, exactly as <see cref="BasicEffect"/> does
/// -- see that type's own doc comment for the trade-off and for the identical, documented gap
/// (<c>CurrentTechnique</c>/<c>Passes</c>, and the directional lights where present, are inherited
/// unchanged and so report <c>CNA.Graphics</c>-namespaced types). Every property here involves only
/// <see cref="Vector3"/>/<see cref="Matrix"/>/<see cref="float"/>/<see cref="bool"/>, which convert
/// implicitly across the boundary, so nothing needs re-typing.</summary>
public class EnvironmentMapEffect : CNA.Graphics.EnvironmentMapEffect, IEffectMatrices, IEffectFog, IEffectLights
{
    public EnvironmentMapEffect(GraphicsDevice graphicsDevice)
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

    Vector3 IEffectLights.AmbientLightColor
    {
        get => base.AmbientLightColor;
        set => base.AmbientLightColor = value;
    }

    bool IEffectLights.LightingEnabled
    {
        get => base.LightingEnabled;
        set => base.LightingEnabled = value;
    }

    DirectionalLight IEffectLights.DirectionalLight0 => DirectionalLight0;

    DirectionalLight IEffectLights.DirectionalLight1 => DirectionalLight1;

    DirectionalLight IEffectLights.DirectionalLight2 => DirectionalLight2;

    void IEffectLights.EnableDefaultLighting() => base.EnableDefaultLighting();

    /// <summary>Re-typed so the compat <see cref="IEffectLights"/> contract is satisfied with this
    /// namespace's <see cref="DirectionalLight"/>. Each wraps the single light object the base
    /// class already constructed rather than building a second one -- see that type's own doc
    /// comment.</summary>
    public new DirectionalLight DirectionalLight0 => _light0 ??= new DirectionalLight(base.DirectionalLight0);

    public new DirectionalLight DirectionalLight1 => _light1 ??= new DirectionalLight(base.DirectionalLight1);

    public new DirectionalLight DirectionalLight2 => _light2 ??= new DirectionalLight(base.DirectionalLight2);

    private DirectionalLight? _light0;
    private DirectionalLight? _light1;
    private DirectionalLight? _light2;

}
