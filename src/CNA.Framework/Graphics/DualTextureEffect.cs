using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>DualTextureEffect</c>: unlit two-layer texturing, classically a base map
/// plus a baked lightmap.
///
/// Both layers are real. <c>cna_dual_texture_effect_get/set_texture</c> take a layer index, so
/// <see cref="Texture"/> is layer 0 and <see cref="Texture2"/> layer 1.
/// </summary>
public class DualTextureEffect : StockEffect, IEffectMatrices, IEffectFog
{
    private Texture? _texture;
    private Texture? _texture2;

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
            SetLayerTexture(0, value, nameof(Texture));
            _texture = value;
        }
    }

    /// <summary>The second texture layer. Real, not a stub: <c>cna_dual_texture_effect_set_texture</c>
    /// takes a layer index ("zero or one" per <c>effects.h:1846</c>). An earlier version of this
    /// binding omitted that parameter and concluded the second layer had no native route, so this
    /// property threw -- a code-review pass found the missing parameter.</summary>
    public Texture? Texture2
    {
        get => _texture2;
        set
        {
            SetLayerTexture(1, value, nameof(Texture2));
            _texture2 = value;
        }
    }

    private void SetLayerTexture(uint layer, Texture? value, string propertyName)
    {
        CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
        CnaResult result = Native.cna_dual_texture_effect_set_texture(Handle, layer, handle);
        GC.KeepAlive(value);
        CnaException.ThrowIfFailed(result, propertyName);
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

    /// <summary>Adopts an already-created native effect -- the clone route's landing point. Private
    /// because a caller has no way to obtain a bare handle; only <see cref="Clone"/> produces one.</summary>
    private DualTextureEffect(GraphicsDevice graphicsDevice, CnaHandle nativeHandle)
        : base(graphicsDevice, nativeHandle)
    {
    }

    /// <summary>An independent copy, matching real XNA. The native clone is documented to be "of
    /// the same concrete native type", which is what makes rewrapping it as
    /// <see cref="DualTextureEffect"/> correct rather than a guess.</summary>
    public override Effect Clone() => new DualTextureEffect(GraphicsDevice, CloneNativeHandle());
}
