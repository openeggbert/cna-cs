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
/// <summary>Matches real XNA's <c>EffectTechnique</c>. Native-backed since Phase 8 WP4a -- its
/// <see cref="Name"/> and <see cref="Passes"/> are the effect's real ones, where previously this
/// type reported a hardcoded <c>"Default"</c> and exactly one fabricated pass. A borrowed handle,
/// same ownership rule as <see cref="EffectParameter"/>.</summary>
public class EffectTechnique : IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectTechnique(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_technique_destroy(new CnaHandle(h)).IsSuccess());
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
    internal CnaHandle NativeHandle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
        _passes?.Dispose();
        _passes = null;
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
                Native.cna_effect_technique_get_name_byte_count, Native.cna_effect_technique_copy_name, NativeHandle, nameof(Name));
            GC.KeepAlive(this);
            return value;
        }
    }

    private EffectPassCollection? _passes;

    /// <summary>Cached and owned, for the reason <see cref="EffectPassCollection"/> spells out: the
    /// native accessor mints a new owned handle per read, and nothing in a ported XNA game ever
    /// disposes a pass collection.</summary>
    public EffectPassCollection Passes
    {
        get
        {
            if (_passes is not null)
            {
                return _passes;
            }

            CnaResult result = Native.cna_effect_technique_get_passes(NativeHandle, out CnaHandle collection);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Passes));
            _passes = new EffectPassCollection(collection);
            return _passes;
        }
    }

    private EffectAnnotationCollection? _annotations;

    public EffectAnnotationCollection Annotations
    {
        get
        {
            if (_annotations is not null)
            {
                return _annotations;
            }

            CnaResult result = Native.cna_effect_technique_get_annotations(NativeHandle, out CnaHandle collection);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            _annotations = new EffectAnnotationCollection(collection);
            return _annotations;
        }
    }
}
