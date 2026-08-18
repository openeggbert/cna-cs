using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>EnvironmentMapEffect</c>: cube-map reflection blended over a
/// base texture. <see cref="EnvironmentMap"/> takes a <see cref="TextureCube"/>, the type WP3b
/// introduced -- which is why this effect could not have been bound before it.</summary>
public class EnvironmentMapEffect : StockEffect, IEffectMatrices, IEffectFog, IEffectLights
{
    private Texture? _texture;
    private TextureCube? _environmentMap;

    public EnvironmentMapEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, CreateNative(graphicsDevice))
    {
        DirectionalLight0 = FetchDirectionalLight(0);
        DirectionalLight1 = FetchDirectionalLight(1);
        DirectionalLight2 = FetchDirectionalLight(2);
    }

    private static CnaHandle CreateNative(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_environment_map_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(EnvironmentMapEffect));
        return handle;
    }

    /// <summary>Each light handle is independently owned and outlives the effect, so each needs its
    /// own destroy call -- confirmed for <see cref="BasicEffect"/> during the native-ABI migration
    /// and equally true here, which is why <see cref="Dispose"/> is overridden.</summary>
    private DirectionalLight FetchDirectionalLight(uint index)
    {
        CnaResult result = Native.cna_effect_lights_get_directional_light(Handle, index, out CnaHandle light);
        CnaException.ThrowIfFailed(result, nameof(EnvironmentMapEffect));
        return new DirectionalLight(light);
    }

    public DirectionalLight DirectionalLight0 { get; }

    public DirectionalLight DirectionalLight1 { get; }

    public DirectionalLight DirectionalLight2 { get; }

    public Vector3 DiffuseColor
    {
        get => GetVector3(Native.cna_environment_map_effect_get_diffuse_color, nameof(DiffuseColor));
        set => SetVector3(Native.cna_environment_map_effect_set_diffuse_color, value, nameof(DiffuseColor));
    }

    public Vector3 EmissiveColor
    {
        get => GetVector3(Native.cna_environment_map_effect_get_emissive_color, nameof(EmissiveColor));
        set => SetVector3(Native.cna_environment_map_effect_set_emissive_color, value, nameof(EmissiveColor));
    }

    public float Alpha
    {
        get => GetFloat(Native.cna_environment_map_effect_get_alpha, nameof(Alpha));
        set => SetFloat(Native.cna_environment_map_effect_set_alpha, value, nameof(Alpha));
    }

    public float EnvironmentMapAmount
    {
        get => GetFloat(Native.cna_environment_map_effect_get_amount, nameof(EnvironmentMapAmount));
        set => SetFloat(Native.cna_environment_map_effect_set_amount, value, nameof(EnvironmentMapAmount));
    }

    public Vector3 EnvironmentMapSpecular
    {
        get => GetVector3(Native.cna_environment_map_effect_get_specular, nameof(EnvironmentMapSpecular));
        set => SetVector3(Native.cna_environment_map_effect_set_specular, value, nameof(EnvironmentMapSpecular));
    }

    public float FresnelFactor
    {
        get => GetFloat(Native.cna_environment_map_effect_get_fresnel_factor, nameof(FresnelFactor));
        set => SetFloat(Native.cna_environment_map_effect_set_fresnel_factor, value, nameof(FresnelFactor));
    }

    public Texture? Texture
    {
        get => _texture;
        set
        {
            SetTexture(Native.cna_environment_map_effect_set_texture, value, nameof(Texture));
            _texture = value;
        }
    }

    public TextureCube? EnvironmentMap
    {
        get => _environmentMap;
        set
        {
            SetTexture(Native.cna_environment_map_effect_set_environment_map, value, nameof(EnvironmentMap));
            _environmentMap = value;
        }
    }

    public Vector3 AmbientLightColor
    {
        get => GetVector3(Native.cna_effect_lights_get_ambient_color, nameof(AmbientLightColor));
        set => SetVector3(Native.cna_effect_lights_set_ambient_color, value, nameof(AmbientLightColor));
    }

    public bool LightingEnabled
    {
        get => GetBool(Native.cna_effect_lights_get_enabled, nameof(LightingEnabled));
        set => SetBool(Native.cna_effect_lights_set_enabled, value, nameof(LightingEnabled));
    }

    public void EnableDefaultLighting()
    {
        CnaResult result = Native.cna_effect_lights_enable_default(Handle);
        CnaException.ThrowIfFailed(result, nameof(EnableDefaultLighting));
    }

    public Matrix World
    {
        get => EffectMatrices.GetWorld(Handle);
        set => EffectMatrices.SetWorld(Handle, value);
    }

    public Matrix View
    {
        get => EffectMatrices.GetView(Handle);
        set => EffectMatrices.SetView(Handle, value);
    }

    public Matrix Projection
    {
        get => EffectMatrices.GetProjection(Handle);
        set => EffectMatrices.SetProjection(Handle, value);
    }

    public bool FogEnabled
    {
        get => GetBool(Native.cna_effect_fog_get_enabled, nameof(FogEnabled));
        set => SetBool(Native.cna_effect_fog_set_enabled, value, nameof(FogEnabled));
    }

    public Vector3 FogColor
    {
        get => GetVector3(Native.cna_effect_fog_get_color, nameof(FogColor));
        set => SetVector3(Native.cna_effect_fog_set_color, value, nameof(FogColor));
    }

    public float FogStart
    {
        get => GetFloat(Native.cna_effect_fog_get_start, nameof(FogStart));
        set => SetFloat(Native.cna_effect_fog_set_start, value, nameof(FogStart));
    }

    public float FogEnd
    {
        get => GetFloat(Native.cna_effect_fog_get_end, nameof(FogEnd));
        set => SetFloat(Native.cna_effect_fog_set_end, value, nameof(FogEnd));
    }

    /// <summary>Destroys the three directional lights before the effect itself -- they are
    /// independently owned handles, not freed implicitly with the effect. Same ordering
    /// <see cref="BasicEffect"/> uses, for the same confirmed reason.</summary>
    public override void Dispose()
    {
        Native.cna_directional_light_destroy(DirectionalLight0.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight1.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight2.NativeHandle);
        base.Dispose();
    }
}
