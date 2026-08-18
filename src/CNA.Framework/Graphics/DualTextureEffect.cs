using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>DualTextureEffect</c>: unlit two-layer texturing, classically a base map
/// plus a baked lightmap.
///
/// <see cref="Texture2"/> is the one member with no native counterpart. The C API exposes a single
/// <c>cna_dual_texture_effect_get/set_texture</c> pair and nothing for the second layer, so
/// <see cref="Texture2"/> throws <see cref="NotSupportedException"/> naming the missing function
/// rather than silently no-opping (which would make a two-layer effect render as one layer with no
/// diagnostic) or being omitted (which would break XNA source that assigns it). This is the rule
/// <c>plan.md</c>'s scope mandate sets for exactly this situation.
/// </summary>
public class DualTextureEffect : StockEffect, IEffectMatrices, IEffectFog
{
    private Texture? _texture;

    public DualTextureEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, CreateNative(graphicsDevice))
    {
    }

    private static CnaHandle CreateNative(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_dual_texture_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(DualTextureEffect));
        return handle;
    }

    public Vector3 DiffuseColor
    {
        get => GetVector3(Native.cna_dual_texture_effect_get_diffuse_color, nameof(DiffuseColor));
        set => SetVector3(Native.cna_dual_texture_effect_set_diffuse_color, value, nameof(DiffuseColor));
    }

    public float Alpha
    {
        get => GetFloat(Native.cna_dual_texture_effect_get_alpha, nameof(Alpha));
        set => SetFloat(Native.cna_dual_texture_effect_set_alpha, value, nameof(Alpha));
    }

    public bool VertexColorEnabled
    {
        get => GetBool(Native.cna_dual_texture_effect_get_vertex_color_enabled, nameof(VertexColorEnabled));
        set => SetBool(Native.cna_dual_texture_effect_set_vertex_color_enabled, value, nameof(VertexColorEnabled));
    }

    public Texture? Texture
    {
        get => _texture;
        set
        {
            SetTexture(Native.cna_dual_texture_effect_set_texture, value, nameof(Texture));
            _texture = value;
        }
    }

    /// <summary>Not supported -- see this class's own doc comment.</summary>
    public Texture? Texture2
    {
        get => throw NotSupported();
        set => throw NotSupported();
    }

    private static NotSupportedException NotSupported() =>
        new("DualTextureEffect.Texture2 has no counterpart in the CNA C API: effects.h exposes only " +
            "cna_dual_texture_effect_get/set_texture for the first layer, with no second-layer function. " +
            "Set the second layer through GraphicsDevice.Textures[1] instead if the renderer supports it.");

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
}
