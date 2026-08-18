namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>IEffectMatrices</c>. A distinct interface from
/// <see cref="CNA.Graphics.IEffectMatrices"/> rather than an alias, because
/// <see cref="Matrix"/> is duplicated per namespace (structs cannot inherit -- see that type's own
/// doc comment). The compat effect classes implement both, forwarding through the implicit
/// conversions, so game code written against either contract works.</summary>
public interface IEffectMatrices
{
    Matrix World { get; set; }

    Matrix View { get; set; }

    Matrix Projection { get; set; }
}

/// <summary>XNA 4.0-compatible <c>IEffectFog</c>. See <see cref="IEffectMatrices"/>.</summary>
public interface IEffectFog
{
    Vector3 FogColor { get; set; }

    bool FogEnabled { get; set; }

    float FogEnd { get; set; }

    float FogStart { get; set; }
}

/// <summary>XNA 4.0-compatible <c>IEffectLights</c>. See <see cref="IEffectMatrices"/>.</summary>
public interface IEffectLights
{
    Vector3 AmbientLightColor { get; set; }

    DirectionalLight DirectionalLight0 { get; }

    DirectionalLight DirectionalLight1 { get; }

    DirectionalLight DirectionalLight2 { get; }

    bool LightingEnabled { get; set; }

    void EnableDefaultLighting();
}
