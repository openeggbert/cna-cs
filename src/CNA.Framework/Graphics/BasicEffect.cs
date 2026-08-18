using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// XNA's built-in general-purpose shader effect (per-vertex lighting, fog, texturing, vertex
/// color). Now a real native object -- the real, shipped openeggbert/cna C API's own
/// <c>cna_basic_effect_create</c> (<c>effects.h</c>) -- rather than the self-designed,
/// zero-ABI-call, all-computed-in-managed-code design this project used before any real ABI
/// existed (see <c>NEXT.md</c>'s native-ABI-migration entry, step 8, for what changed and why).
/// Every property below is now a real, immediate native round trip: getting or setting
/// <see cref="World"/>, <see cref="DiffuseColor"/>, and so on each cross the ABI on their own,
/// rather than being cached in a plain C# field/property and pushed to native only once, inside
/// <see cref="OnApply"/> -- matching the real ABI's own shape (every one of these is a real,
/// separate <c>cna_*_get_*</c>/<c>_set_*</c> function pair), not a design choice this migration
/// invented. <see cref="OnApply"/> itself is now trivial: <c>cna_effect_apply</c> and nothing else
/// -- all of the derived-parameter math the old design needed
/// (diffuse/emissive/specular/eye-position blending, the fog-vector derivation, the
/// row-to-column-major matrix rewrite) is now native's own job, computed from whatever the
/// properties below were last set to.
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
public class BasicEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights
{
    private readonly CnaHandle _handle;

    protected internal override nint NativeEffectHandleValue => _handle.AsNint;
    private Texture? _texture;
    private bool _disposed;

    public BasicEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        CnaResult result = Native.cna_basic_effect_create(graphicsDevice.ResolveNativeDeviceHandle(), out _handle);
        CnaException.ThrowIfFailed(result, nameof(BasicEffect));

        // Confirmed directly against BasicEffectSmoke.c: each of these three fetches an
        // independently owned handle -- it survives even if the parent effect is destroyed first --
        // so each needs its own destroy call, done in this class's own Dispose below, not left to
        // be freed implicitly with the effect.
        DirectionalLight0 = FetchDirectionalLight(0);
        DirectionalLight1 = FetchDirectionalLight(1);
        DirectionalLight2 = FetchDirectionalLight(2);
    }

    private DirectionalLight FetchDirectionalLight(uint index)
    {
        CnaResult result = Native.cna_effect_lights_get_directional_light(_handle, index, out CnaHandle light);
        CnaException.ThrowIfFailed(result, nameof(BasicEffect));
        return new DirectionalLight(light);
    }

    public DirectionalLight DirectionalLight0 { get; }

    public DirectionalLight DirectionalLight1 { get; }

    public DirectionalLight DirectionalLight2 { get; }

    public Matrix World
    {
        get
        {
            CnaResult result = Native.cna_effect_matrices_get_world(_handle, out CnaMatrix value);
            CnaException.ThrowIfFailed(result, nameof(World));
            return Matrix.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_effect_matrices_set_world(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(World));
        }
    }

    public Matrix View
    {
        get
        {
            CnaResult result = Native.cna_effect_matrices_get_view(_handle, out CnaMatrix value);
            CnaException.ThrowIfFailed(result, nameof(View));
            return Matrix.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_effect_matrices_set_view(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(View));
        }
    }

    public Matrix Projection
    {
        get
        {
            CnaResult result = Native.cna_effect_matrices_get_projection(_handle, out CnaMatrix value);
            CnaException.ThrowIfFailed(result, nameof(Projection));
            return Matrix.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_effect_matrices_set_projection(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(Projection));
        }
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
        get
        {
            CnaResult result = Native.cna_basic_effect_get_specular_power(_handle, out float value);
            CnaException.ThrowIfFailed(result, nameof(SpecularPower));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_basic_effect_set_specular_power(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(SpecularPower));
        }
    }

    public Vector3 AmbientLightColor
    {
        get => GetVector3(Native.cna_effect_lights_get_ambient_color, nameof(AmbientLightColor));
        set => SetVector3(Native.cna_effect_lights_set_ambient_color, value, nameof(AmbientLightColor));
    }

    public float Alpha
    {
        get
        {
            CnaResult result = Native.cna_basic_effect_get_alpha(_handle, out float value);
            CnaException.ThrowIfFailed(result, nameof(Alpha));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_basic_effect_set_alpha(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(Alpha));
        }
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
            CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
            CnaResult result = Native.cna_basic_effect_set_texture(_handle, handle);
            GC.KeepAlive(value);
            CnaException.ThrowIfFailed(result, nameof(Texture));
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
        get
        {
            CnaResult result = Native.cna_effect_fog_get_start(_handle, out float value);
            CnaException.ThrowIfFailed(result, nameof(FogStart));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_effect_fog_set_start(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(FogStart));
        }
    }

    public float FogEnd
    {
        get
        {
            CnaResult result = Native.cna_effect_fog_get_end(_handle, out float value);
            CnaException.ThrowIfFailed(result, nameof(FogEnd));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_effect_fog_set_end(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(FogEnd));
        }
    }

    /// <summary>Matches real XNA's own convenience method exactly -- now a single native call
    /// (<c>cna_effect_lights_enable_default</c>) instead of the 20-odd hardcoded literals the old,
    /// self-designed version needed to reproduce the real default three-point lighting rig by
    /// hand.</summary>
    public void EnableDefaultLighting()
    {
        CnaResult result = Native.cna_effect_lights_enable_default(_handle);
        CnaException.ThrowIfFailed(result, nameof(EnableDefaultLighting));
    }

    /// <summary>Selects this effect on its owning graphics device -- all of the derived-parameter
    /// computation the old design needed here (diffuse/emissive/specular/eye-position blending,
    /// fog-vector derivation, row-to-column-major matrix rewrite) is native's own job now, computed
    /// from whatever this effect's properties were last set to.</summary>
    protected override void OnApply()
    {
        CnaResult result = Native.cna_effect_apply(_handle);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Native.cna_directional_light_destroy(DirectionalLight0.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight1.NativeHandle);
        Native.cna_directional_light_destroy(DirectionalLight2.NativeHandle);
        Native.cna_effect_destroy(_handle);
        base.Dispose();
    }

    private bool GetBool(GetBoolFunc getter, string propertyName)
    {
        CnaResult result = getter(_handle, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return value != 0;
    }

    private void SetBool(SetBoolFunc setter, bool value, string propertyName)
    {
        CnaResult result = setter(_handle, value ? (byte)1 : (byte)0);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    private Vector3 GetVector3(GetVector3Func getter, string propertyName)
    {
        CnaResult result = getter(_handle, out CnaVector3 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return Vector3.FromNative(value);
    }

    private void SetVector3(SetVector3Func setter, Vector3 value, string propertyName)
    {
        CnaResult result = setter(_handle, value.ToNative());
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    private delegate CnaResult GetBoolFunc(CnaHandle effect, out byte outValue);
    private delegate CnaResult SetBoolFunc(CnaHandle effect, byte value);
    private delegate CnaResult GetVector3Func(CnaHandle effect, out CnaVector3 outValue);
    private delegate CnaResult SetVector3Func(CnaHandle effect, CnaVector3 value);
}
