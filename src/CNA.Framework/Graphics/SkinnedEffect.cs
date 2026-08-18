using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>SkinnedEffect</c>: per-vertex skeletal animation, where each vertex is
/// weighted against up to <see cref="WeightsPerVertex"/> bones and transformed by
/// <see cref="SetBoneTransforms"/>.
///
/// Its arrival unblocks a long-standing deferral: <c>.cnj</c> model loading has always rejected
/// skinned documents because, as that code's own note put it, skinning had "no real payoff without
/// a SkinnedEffect type, which doesn't exist anywhere in this project". It does now -- see
/// <c>plan.md</c> WP15.
/// </summary>
public class SkinnedEffect : StockEffect, IEffectMatrices, IEffectFog, IEffectLights
{
    /// <summary>Real XNA's own documented ceiling on the bone-transform array. Kept as a named
    /// constant because <see cref="SetBoneTransforms"/> validates against it rather than letting
    /// native reject an oversized array with a less specific message.</summary>
    public const int MaxBones = 72;

    private Texture? _texture;

    public SkinnedEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, CreateNative(graphicsDevice))
    {
        DirectionalLight0 = FetchDirectionalLight(0);
        DirectionalLight1 = FetchDirectionalLight(1);
        DirectionalLight2 = FetchDirectionalLight(2);
    }

    private static CnaHandle CreateNative(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_skinned_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(SkinnedEffect));
        return handle;
    }

    private DirectionalLight FetchDirectionalLight(uint index)
    {
        CnaResult result = Native.cna_effect_lights_get_directional_light(Handle, index, out CnaHandle light);
        CnaException.ThrowIfFailed(result, nameof(SkinnedEffect));
        return new DirectionalLight(light);
    }

    public DirectionalLight DirectionalLight0 { get; }

    public DirectionalLight DirectionalLight1 { get; }

    public DirectionalLight DirectionalLight2 { get; }

    public Vector3 DiffuseColor
    {
        get => GetVector3(Native.cna_skinned_effect_get_diffuse_color, nameof(DiffuseColor));
        set => SetVector3(Native.cna_skinned_effect_set_diffuse_color, value, nameof(DiffuseColor));
    }

    public Vector3 EmissiveColor
    {
        get => GetVector3(Native.cna_skinned_effect_get_emissive_color, nameof(EmissiveColor));
        set => SetVector3(Native.cna_skinned_effect_set_emissive_color, value, nameof(EmissiveColor));
    }

    public Vector3 SpecularColor
    {
        get => GetVector3(Native.cna_skinned_effect_get_specular_color, nameof(SpecularColor));
        set => SetVector3(Native.cna_skinned_effect_set_specular_color, value, nameof(SpecularColor));
    }

    public float SpecularPower
    {
        get => GetFloat(Native.cna_skinned_effect_get_specular_power, nameof(SpecularPower));
        set => SetFloat(Native.cna_skinned_effect_set_specular_power, value, nameof(SpecularPower));
    }

    public float Alpha
    {
        get => GetFloat(Native.cna_skinned_effect_get_alpha, nameof(Alpha));
        set => SetFloat(Native.cna_skinned_effect_set_alpha, value, nameof(Alpha));
    }

    public bool PreferPerPixelLighting
    {
        get => GetBool(Native.cna_skinned_effect_get_prefer_per_pixel_lighting, nameof(PreferPerPixelLighting));
        set => SetBool(Native.cna_skinned_effect_set_prefer_per_pixel_lighting, value, nameof(PreferPerPixelLighting));
    }

    public bool VertexColorEnabled
    {
        get => GetBool(Native.cna_skinned_effect_get_vertex_color_enabled, nameof(VertexColorEnabled));
        set => SetBool(Native.cna_skinned_effect_set_vertex_color_enabled, value, nameof(VertexColorEnabled));
    }

    /// <summary>How many bones influence each vertex: 1, 2 or 4 in real XNA.</summary>
    public int WeightsPerVertex
    {
        get => GetInt(Native.cna_skinned_effect_get_weights_per_vertex, nameof(WeightsPerVertex));
        set => SetInt(Native.cna_skinned_effect_set_weights_per_vertex, value, nameof(WeightsPerVertex));
    }

    public Texture? Texture
    {
        get => _texture;
        set
        {
            SetTexture(Native.cna_skinned_effect_set_texture, value, nameof(Texture));
            _texture = value;
        }
    }

    public unsafe void SetBoneTransforms(Matrix[] boneTransforms)
    {
        ArgumentNullException.ThrowIfNull(boneTransforms);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boneTransforms.Length, MaxBones);

        var native = new CnaMatrix[boneTransforms.Length];
        for (int i = 0; i < native.Length; i++)
        {
            native[i] = boneTransforms[i].ToNative();
        }

        fixed (CnaMatrix* nativePtr = native)
        {
            CnaResult result = Native.cna_skinned_effect_set_bone_transforms(Handle, nativePtr, (ulong)native.Length);
            CnaException.ThrowIfFailed(result, nameof(SetBoneTransforms));
        }
    }

    /// <summary>Matches real XNA's <c>GetBoneTransforms(int count)</c>. The native side is a
    /// size-aware copy (<c>cna_skinned_effect_copy_bone_transforms</c> reports how many it actually
    /// wrote), so the returned array is trimmed to that rather than to the requested
    /// count.</summary>
    public unsafe Matrix[] GetBoneTransforms(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxBones);

        if (count == 0)
        {
            return [];
        }

        var native = new CnaMatrix[count];
        fixed (CnaMatrix* nativePtr = native)
        {
            CnaResult result = Native.cna_skinned_effect_copy_bone_transforms(
                Handle, (ulong)count, nativePtr, (ulong)count, out ulong written);
            CnaException.ThrowIfFailed(result, nameof(GetBoneTransforms));

            var transforms = new Matrix[written];
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i] = Matrix.FromNative(native[i]);
            }

            return transforms;
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

    public override void Dispose()
    {
        Native.cna_directional_light_destroy(DirectionalLight0.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight1.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight2.NativeHandle);
        base.Dispose();
    }
}
