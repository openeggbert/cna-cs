using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// XNA's built-in general-purpose shader effect (per-vertex lighting, fog, texturing, vertex
/// color). A real native object -- the real, shipped openeggbert/cna C API's own
/// <c>cna_basic_effect_create</c> (<c>effects.h</c>) -- rather than the self-designed,
/// zero-ABI-call, all-computed-in-managed-code design this project used before any real ABI
/// existed (see <c>NEXT.md</c>'s native-ABI-migration entry, step 8, for what changed and why).
/// Every property below is a real, immediate native round trip: getting or setting
/// <see cref="World"/>, <see cref="DiffuseColor"/>, and so on each cross the ABI on their own,
/// rather than being cached in a plain C# field/property and pushed to native only once, inside
/// <c>OnApply</c> -- matching the real ABI's own shape (every one of these is a real,
/// separate <c>cna_*_get_*</c>/<c>_set_*</c> function pair), not a design choice this migration
/// invented. Applying is trivial: <c>cna_effect_apply</c> and nothing else
/// -- all of the derived-parameter math the old design needed
/// (diffuse/emissive/specular/eye-position blending, the fog-vector derivation, the
/// row-to-column-major matrix rewrite) is native's own job, computed from whatever the
/// properties below were last set to.
///
/// Derives from <see cref="StockEffect"/> since WP15, which is where the handle, its disposal,
/// <c>Apply</c>, the get/set helpers and the directional-light rig now live. Folding it in was not
/// only de-duplication: this class used to hold a bare <see cref="CnaHandle"/> where the other four
/// stock effects hold an owned
/// <see cref="System.Runtime.InteropServices.SafeHandle"/>, so an undisposed
/// <see cref="BasicEffect"/> leaked its effect plus three light handles for the process lifetime.
/// The model builders create one of these per mesh part.
///
/// Implements <see cref="IEffectMatrices"/>/<see cref="IEffectFog"/>/<see cref="IEffectLights"/>,
/// same as the real C++ engine's own <c>BasicEffect</c> -- <see cref="Model.Draw"/> is the reason
/// <see cref="IEffectMatrices"/> exists at all in this project. <see cref="IEffectFog"/>/
/// <see cref="IEffectLights"/>'s members already match this class's own property names/types
/// exactly, so they're implicitly satisfied; only <see cref="IEffectMatrices"/> needs an explicit
/// forwarding implementation, because <see cref="World"/>/<see cref="View"/>/<see cref="Projection"/>
/// are properties here (fields, before this migration, matching the real C++ engine's own
/// field-not-property choice -- no longer possible once each one needs to trigger a native call on
/// every write) and the explicit interface members just forward to them.
/// </summary>
public class BasicEffect : StockEffect, IEffectMatrices, IEffectFog, IEffectLights
{
    private Texture? _texture;

    public BasicEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, CreateNative(graphicsDevice))
    {
        DirectionalLight0 = FetchDirectionalLight(0);
        DirectionalLight1 = FetchDirectionalLight(1);
        DirectionalLight2 = FetchDirectionalLight(2);
    }

    private static CnaHandle CreateNative(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        CnaResult result = Native.cna_basic_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(BasicEffect));
        return handle;
    }

    public DirectionalLight DirectionalLight0 { get; }

    public DirectionalLight DirectionalLight1 { get; }

    public DirectionalLight DirectionalLight2 { get; }

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

    public bool VertexColorEnabled
    {
        get => GetBool(Native.cna_basic_effect_get_vertex_color_enabled, nameof(VertexColorEnabled));
        set => SetBool(Native.cna_basic_effect_set_vertex_color_enabled, value, nameof(VertexColorEnabled));
    }

    public bool PreferPerPixelLighting
    {
        get => GetBool(Native.cna_basic_effect_get_prefer_per_pixel_lighting, nameof(PreferPerPixelLighting));
        set => SetBool(Native.cna_basic_effect_set_prefer_per_pixel_lighting, value, nameof(PreferPerPixelLighting));
    }

    public Vector3 DiffuseColor
    {
        get => GetVector3(Native.cna_basic_effect_get_diffuse_color, nameof(DiffuseColor));
        set => SetVector3(Native.cna_basic_effect_set_diffuse_color, value, nameof(DiffuseColor));
    }

    public Vector3 EmissiveColor
    {
        get => GetVector3(Native.cna_basic_effect_get_emissive_color, nameof(EmissiveColor));
        set => SetVector3(Native.cna_basic_effect_set_emissive_color, value, nameof(EmissiveColor));
    }

    public Vector3 SpecularColor
    {
        get => GetVector3(Native.cna_basic_effect_get_specular_color, nameof(SpecularColor));
        set => SetVector3(Native.cna_basic_effect_set_specular_color, value, nameof(SpecularColor));
    }

    public float SpecularPower
    {
        get => GetFloat(Native.cna_basic_effect_get_specular_power, nameof(SpecularPower));
        set => SetFloat(Native.cna_basic_effect_set_specular_power, value, nameof(SpecularPower));
    }

    public Vector3 AmbientLightColor
    {
        get => GetVector3(Native.cna_effect_lights_get_ambient_color, nameof(AmbientLightColor));
        set => SetVector3(Native.cna_effect_lights_set_ambient_color, value, nameof(AmbientLightColor));
    }

    public float Alpha
    {
        get => GetFloat(Native.cna_basic_effect_get_alpha, nameof(Alpha));
        set => SetFloat(Native.cna_basic_effect_set_alpha, value, nameof(Alpha));
    }

    public bool LightingEnabled
    {
        get => GetBool(Native.cna_effect_lights_get_enabled, nameof(LightingEnabled));
        set => SetBool(Native.cna_effect_lights_set_enabled, value, nameof(LightingEnabled));
    }

    public bool TextureEnabled
    {
        get => GetBool(Native.cna_basic_effect_get_texture_enabled, nameof(TextureEnabled));
        set => SetBool(Native.cna_basic_effect_set_texture_enabled, value, nameof(TextureEnabled));
    }

    /// <summary>
    /// Setting this also calls <c>cna_basic_effect_set_texture</c> immediately, retaining or
    /// clearing the native effect's own texture reference -- but the getter returns this project's
    /// own cached <see cref="Texture2D"/> reference rather than round-tripping through
    /// <c>cna_basic_effect_get_texture</c> (which only ever answers a raw <see cref="CnaHandle"/>,
    /// not a <see cref="Texture2D"/> wrapper this project could safely reconstruct without risking
    /// a double-ownership bug over whichever wrapper actually owns that handle's disposal).
    /// </summary>
    public Texture? Texture
    {
        get => _texture;
        set
        {
            SetTexture(Native.cna_basic_effect_set_texture, value, nameof(Texture));
            _texture = value;
        }
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

    Matrix IEffectMatrices.World
    {
        get => World;
        set => World = value;
    }

    Matrix IEffectMatrices.View
    {
        get => View;
        set => View = value;
    }

    Matrix IEffectMatrices.Projection
    {
        get => Projection;
        set => Projection = value;
    }

    /// <summary>Matches real XNA's own convenience method exactly -- a single native call
    /// (<c>cna_effect_lights_enable_default</c>) instead of the 20-odd hardcoded literals the old,
    /// self-designed version needed to reproduce the real default three-point lighting rig by
    /// hand.</summary>
    public void EnableDefaultLighting()
    {
        CnaResult result = Native.cna_effect_lights_enable_default(Handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(EnableDefaultLighting));
    }

    private protected override void ReleaseAdditionalNativeResources() =>
        ReleaseDirectionalLights(DirectionalLight0, DirectionalLight1, DirectionalLight2);

    /// <summary>Adopts an already-created native effect -- the clone route's landing point. Private
    /// because a caller has no way to obtain a bare handle; only <see cref="Clone"/> produces one.</summary>
    private BasicEffect(GraphicsDevice graphicsDevice, CnaHandle nativeHandle)
        : base(graphicsDevice, nativeHandle)
    {
        DirectionalLight0 = FetchDirectionalLight(0);
        DirectionalLight1 = FetchDirectionalLight(1);
        DirectionalLight2 = FetchDirectionalLight(2);
    }

    /// <summary>An independent copy, matching real XNA. The native clone is documented to be "of
    /// the same concrete native type", which is what makes rewrapping it as
    /// <see cref="BasicEffect"/> correct rather than a guess.</summary>
    public override Effect Clone() => new BasicEffect(GraphicsDevice, CloneNativeHandle());
}
