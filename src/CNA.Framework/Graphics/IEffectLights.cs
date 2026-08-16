namespace CNA.Graphics;

/// <summary>
/// Real XNA interface for effects that support up to three directional lights plus ambient.
/// Confirmed against the real openeggbert/cna C++ engine's own <c>IEffectLights</c> -- not
/// invented. <see cref="BasicEffect"/> already has every one of these members with matching
/// names/types, so implementing it costs nothing beyond declaring the interface.
/// </summary>
public interface IEffectLights
{
    Vector3 AmbientLightColor { get; set; }

    DirectionalLight DirectionalLight0 { get; }

    DirectionalLight DirectionalLight1 { get; }

    DirectionalLight DirectionalLight2 { get; }

    bool LightingEnabled { get; set; }

    void EnableDefaultLighting();
}
