using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Shared base for the native-backed stock effects added by Phase 8 WP4b
/// (<see cref="AlphaTestEffect"/>, <see cref="DualTextureEffect"/>,
/// <see cref="EnvironmentMapEffect"/>, <see cref="SkinnedEffect"/>): it owns the
/// <c>CNA_EffectHandle</c>, its disposal, <c>Apply</c>, and the small get/set helpers each of
/// those four would otherwise repeat.
///
/// <see cref="BasicEffect"/> deliberately does *not* derive from this yet. It predates this class
/// and carries its own copy of the same handle-and-helpers shape plus three independently owned
/// <see cref="DirectionalLight"/> handles with their own disposal rules; migrating it is a real
/// (small) refactor of already-reviewed working code, so it belongs in its own increment rather
/// than riding along with four new types. Tracked in <c>plan.md</c> WP15.
/// </summary>
public abstract class StockEffect : Effect
{
    private readonly NativeResourceHandle _ownedHandle;
    private bool _disposed;

    private protected StockEffect(GraphicsDevice graphicsDevice, CnaHandle handle)
        : base(graphicsDevice)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_destroy(new CnaHandle(h)));
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
        CnaException.ThrowIfFailed(result, nameof(Apply));
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
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
    }
}
