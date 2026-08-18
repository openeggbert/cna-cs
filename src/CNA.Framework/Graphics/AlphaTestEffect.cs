using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>AlphaTestEffect</c>: unlit texturing with an alpha test, driven
/// by <see cref="AlphaFunction"/> and <see cref="ReferenceAlpha"/>. World/View/Projection and fog
/// come from the shared <see cref="IEffectMatrices"/>/<see cref="IEffectFog"/> contracts, the same
/// ones <see cref="BasicEffect"/> uses -- they are effect-wide in this ABI, not
/// per-effect-type.</summary>
public class AlphaTestEffect : StockEffect, IEffectMatrices, IEffectFog
{
    private Texture? _texture;

    public AlphaTestEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, CreateNative(graphicsDevice))
    {
    }

    private static CnaHandle CreateNative(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_alpha_test_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(AlphaTestEffect));
        return handle;
    }

    public Vector3 DiffuseColor
    {
        get => GetVector3(Native.cna_alpha_test_effect_get_diffuse_color, nameof(DiffuseColor));
        set => SetVector3(Native.cna_alpha_test_effect_set_diffuse_color, value, nameof(DiffuseColor));
    }

    public float Alpha
    {
        get => GetFloat(Native.cna_alpha_test_effect_get_alpha, nameof(Alpha));
        set => SetFloat(Native.cna_alpha_test_effect_set_alpha, value, nameof(Alpha));
    }

    public bool VertexColorEnabled
    {
        get => GetBool(Native.cna_alpha_test_effect_get_vertex_color_enabled, nameof(VertexColorEnabled));
        set => SetBool(Native.cna_alpha_test_effect_set_vertex_color_enabled, value, nameof(VertexColorEnabled));
    }

    public int ReferenceAlpha
    {
        get => GetInt(Native.cna_alpha_test_effect_get_reference_alpha, nameof(ReferenceAlpha));
        set => SetInt(Native.cna_alpha_test_effect_set_reference_alpha, value, nameof(ReferenceAlpha));
    }

    public CompareFunction AlphaFunction
    {
        get
        {
            CnaResult result = Native.cna_alpha_test_effect_get_alpha_function(Handle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(AlphaFunction));
            return (CompareFunction)value;
        }
        set
        {
            CnaResult result = Native.cna_alpha_test_effect_set_alpha_function(Handle, (uint)value);
            CnaException.ThrowIfFailed(result, nameof(AlphaFunction));
        }
    }

    public Texture? Texture
    {
        get => _texture;
        set
        {
            SetTexture(Native.cna_alpha_test_effect_set_texture, value, nameof(Texture));
            _texture = value;
        }
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
    private AlphaTestEffect(GraphicsDevice graphicsDevice, CnaHandle nativeHandle)
        : base(graphicsDevice, nativeHandle)
    {
    }

    /// <summary>An independent copy, matching real XNA. The native clone is documented to be "of
    /// the same concrete native type", which is what makes rewrapping it as
    /// <see cref="AlphaTestEffect"/> correct rather than a guess.</summary>
    public override Effect Clone() => new AlphaTestEffect(GraphicsDevice, CloneNativeHandle());
}
