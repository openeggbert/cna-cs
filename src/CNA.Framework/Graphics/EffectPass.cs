using CNA;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Owns its native handle. <c>effects.h</c> documents every handle the reflection API hands out as
/// "Owned" -- a fresh registry slot per call, not an alias of something the effect owns -- and
/// declares a matching <c>destroy</c> for each. An earlier revision of this binding asserted the
/// opposite and destroyed none of them, which leaked on the per-frame
/// <c>ModelMesh.Draw</c> path (one technique + one pass collection + N passes per draw). Found by a
/// code-review pass.
///
/// Release runs through <see cref="NativeResourceHandle"/>, a <see cref="System.Runtime.InteropServices.SafeHandle"/>,
/// so the GC reclaims these even though real XNA's equivalents are not <see cref="IDisposable"/>
/// and callers never dispose them. <see cref="IDisposable"/> is offered too, for a caller that
/// wants the handle back promptly.
/// </summary>
/// <summary>Matches real XNA's <c>EffectPass</c>. Native-backed since Phase 8 WP4a: <see cref="Apply"/>
/// calls <c>cna_effect_pass_apply</c> for this specific pass, where previously it forwarded to the
/// whole effect's <c>Apply()</c> because no per-pass native object was bound -- which made a
/// multi-pass technique silently render only whatever the effect's single apply did.</summary>
public class EffectPass : IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;
    private EffectAnnotationCollection? _annotations;

    internal EffectPass(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_pass_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>.
    ///
    /// Every caller pairs this with <see cref="GC.KeepAlive(object)"/> after the native call. That
    /// is not decoration: these wrappers are routinely temporaries -- <c>effect.Parameters["World"]
    /// .SetValue(m)</c> leaves the <see cref="EffectParameter"/> unreachable the moment its handle
    /// has been read -- and the moment they are unreachable the <see cref="System.Runtime.InteropServices.SafeHandle"/>
    /// finalizer is free to run <c>destroy</c> while the native call is still in flight. Giving
    /// these types SafeHandle ownership is what fixed their leak; it is also what introduced this
    /// hazard, since before that they held a bare handle with no finalizer at all.
    ///
    /// <see cref="GC.KeepAlive(object)"/> closes the ordinary reachability hazard. In addition,
    /// <see cref="NativeResourceHandle"/> defers finalizer-thread and cross-thread releases to an
    /// owner-thread safe point, so an unreachable wrapper cannot destroy this raw handle during a
    /// native call. This project still does not promise concurrent <c>Dispose</c>/operation safety.
    /// </summary>
    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
        _annotations?.Dispose();
        _annotations = null;
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public unsafe string Name
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_effect_pass_get_name_byte_count, Native.cna_effect_pass_copy_name, _handle, nameof(Name));
            GC.KeepAlive(this);
            return value;
        }
    }

    public EffectAnnotationCollection Annotations
    {
        get
        {
            if (_annotations is not null)
            {
                return _annotations;
            }

            CnaResult result = Native.cna_effect_pass_get_annotations(_handle, out CnaHandle collection);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            _annotations = new EffectAnnotationCollection(collection);
            return _annotations;
        }
    }

    public void Apply()
    {
        CnaResult result = Native.cna_effect_pass_apply(_handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }
}
