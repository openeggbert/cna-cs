using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Shared base for the native-backed stock effects (<see cref="BasicEffect"/>,
/// <see cref="AlphaTestEffect"/>, <see cref="DualTextureEffect"/>,
/// <see cref="EnvironmentMapEffect"/>, <see cref="SkinnedEffect"/>): it owns the
/// <c>CNA_EffectHandle</c>, its disposal, <c>Apply</c>, the small get/set helpers, and the
/// three-point directional-light rig, each of which the five would otherwise repeat.
///
/// <see cref="BasicEffect"/> was the last to move here, in WP15. It predated this class and carried
/// its own copy of the same shape, which cost more than duplication: its handle was a bare
/// <see cref="CnaHandle"/> rather than a
/// <see cref="System.Runtime.InteropServices.SafeHandle"/>, so an undisposed
/// <see cref="BasicEffect"/> leaked its effect and all three lights for the process lifetime --
/// exactly what this class's owned handle exists to prevent.
/// </summary>
public abstract class StockEffect : Effect
{
    private readonly NativeResourceHandle _ownedHandle;
    private bool _disposed;

    private protected StockEffect(GraphicsDevice graphicsDevice, CnaHandle handle)
        : base(graphicsDevice)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>
    /// Held in a <see cref="NativeResourceHandle"/> rather than as a bare <see cref="CnaHandle"/>,
    /// so a <see cref="System.Runtime.InteropServices.SafeHandle"/> finalizer reclaims the effect
    /// even when nothing disposes it. That matters concretely: the model builders create one
    /// <see cref="BasicEffect"/> per mesh part, and <c>Model</c>/<c>ModelMesh</c> have no
    /// <c>Dispose</c> at all, so before this every loaded model leaked an effect (plus its three
    /// directional lights) per part for the process lifetime. Found by a code-review pass; a real
    /// <c>Model.Dispose</c> is the proper fix and is tracked in <c>plan.md</c> WP15.
    /// </summary>
    private protected CnaHandle Handle => new(_ownedHandle.DangerousGetHandle());

    protected internal override nint NativeEffectHandleValue => Handle.AsNint;

    /// <summary>Selects this effect on its owning device. Every stock effect shares
    /// <c>cna_effect_apply</c> -- there is no per-effect-type apply.</summary>
    protected override void OnApply()
    {
        CnaResult result = Native.cna_effect_apply(Handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }

    /// <summary>
    /// Fetches one of the effect's three directional lights.
    ///
    /// Confirmed against <c>BasicEffectSmoke.c</c>: each fetch returns an *independently owned*
    /// handle that survives its parent effect being destroyed first, so each needs its own
    /// <c>cna_directional_light_destroy</c> rather than being freed implicitly with the effect.
    /// The returned <see cref="DirectionalLight"/> owns that handle, so the destroy happens either
    /// through <see cref="ReleaseDirectionalLights"/> on disposal or through its critical finalizer
    /// if nothing ever disposes the effect. A lit effect pairs the two.
    /// </summary>
    private protected DirectionalLight FetchDirectionalLight(uint index)
    {
        CnaResult result = Native.cna_effect_lights_get_directional_light(Handle, index, out CnaHandle light);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(FetchDirectionalLight));
        return new DirectionalLight(light);
    }

    /// <summary>Releases the three lights a lit effect fetched -- see
    /// <see cref="FetchDirectionalLight"/> for why they are not freed with the effect. Called from
    /// <see cref="ReleaseAdditionalNativeResources"/>, so it inherits that hook's disposal guard;
    /// each light also owns its handle, so an effect that is never disposed at all still has them
    /// reclaimed by the critical finalizer rather than leaking.</summary>
    private protected static void ReleaseDirectionalLights(
        DirectionalLight light0, DirectionalLight light1, DirectionalLight light2)
    {
        light0.ReleaseNative();
        light1.ReleaseNative();
        light2.ReleaseNative();
    }

    /// <summary>Releases handles a subclass owns *besides* the effect itself -- the three
    /// independently-owned <see cref="DirectionalLight"/>s the lit effects fetch, which are not
    /// freed implicitly with the effect.
    ///
    /// A hook rather than a <see cref="Dispose"/> override on purpose: overriding <c>Dispose</c>
    /// puts the subclass's cleanup *before* this class's <c>_disposed</c> guard, so a second
    /// <c>Dispose()</c> double-frees those handles while the effect handle itself stays correctly
    /// guarded. <see cref="EnvironmentMapEffect"/> and <see cref="SkinnedEffect"/> both had exactly
    /// that bug until a code-review pass found it; this hook makes it unrepresentable rather than
    /// something each subclass has to remember.</summary>
    private protected virtual void ReleaseAdditionalNativeResources()
    {
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Effect owns the reflection handles it caches (parameters, techniques, passes, and the
        // current technique). Skipping the base call left those children alive after the stock
        // effect itself was destroyed; native then correctly refused to destroy the owning game.
        // This surfaced when a compat BasicEffect applied CurrentTechnique.Passes[0] and the next
        // test attempted to create a game in the same process.
        base.Dispose();
        ReleaseAdditionalNativeResources();
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    private protected delegate CnaResult GetBoolFunc(CnaHandle effect, out byte outValue);

    private protected delegate CnaResult SetBoolFunc(CnaHandle effect, byte value);

    private protected delegate CnaResult GetFloatFunc(CnaHandle effect, out float outValue);

    private protected delegate CnaResult SetFloatFunc(CnaHandle effect, float value);

    private protected delegate CnaResult GetIntFunc(CnaHandle effect, out int outValue);

    private protected delegate CnaResult SetIntFunc(CnaHandle effect, int value);

    private protected delegate CnaResult GetVector3Func(CnaHandle effect, out CnaVector3 outValue);

    private protected delegate CnaResult SetVector3Func(CnaHandle effect, CnaVector3 value);

    private protected delegate CnaResult SetTextureFunc(CnaHandle effect, CnaHandle texture);

    private protected bool GetBool(GetBoolFunc getter, string propertyName)
    {
        CnaResult result = getter(Handle, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return value != 0;
    }

    private protected void SetBool(SetBoolFunc setter, bool value, string propertyName)
    {
        CnaResult result = setter(Handle, (byte)(value ? 1 : 0));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    private protected float GetFloat(GetFloatFunc getter, string propertyName)
    {
        CnaResult result = getter(Handle, out float value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return value;
    }

    private protected void SetFloat(SetFloatFunc setter, float value, string propertyName)
    {
        CnaResult result = setter(Handle, value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    private protected int GetInt(GetIntFunc getter, string propertyName)
    {
        CnaResult result = getter(Handle, out int value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return value;
    }

    private protected void SetInt(SetIntFunc setter, int value, string propertyName)
    {
        CnaResult result = setter(Handle, value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    private protected Vector3 GetVector3(GetVector3Func getter, string propertyName)
    {
        CnaResult result = getter(Handle, out CnaVector3 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return Vector3.FromNative(value);
    }

    private protected void SetVector3(SetVector3Func setter, Vector3 value, string propertyName)
    {
        CnaResult result = setter(Handle, value.ToNative());
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    /// <summary>Texture setters take a handle; the managed reference is kept by the caller so the
    /// getter can hand back the same object it was given. The native getter reports a handle this
    /// project has no way to map back to its managed wrapper -- the same limitation
    /// <see cref="TextureCollection"/> documents -- which is why every stock effect caches its
    /// texture reference rather than re-reading it.</summary>
    private protected void SetTexture(SetTextureFunc setter, Texture? value, string propertyName)
    {
        CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
        CnaResult result = setter(Handle, handle);
        GC.KeepAlive(value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }
}
