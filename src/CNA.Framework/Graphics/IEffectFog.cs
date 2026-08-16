namespace CNA.Graphics;

/// <summary>
/// Real XNA interface for effects that support distance-based linear fog. Confirmed against the
/// real openeggbert/cna C++ engine's own <c>IEffectFog</c> -- not invented. <see cref="BasicEffect"/>
/// already has every one of these members with matching names/types, so implementing it costs
/// nothing beyond declaring the interface.
/// </summary>
public interface IEffectFog
{
    Vector3 FogColor { get; set; }

    bool FogEnabled { get; set; }

    float FogEnd { get; set; }

    float FogStart { get; set; }
}
