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
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_technique_destroy(new CnaHandle(h)));
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
    /// <see cref="GC.KeepAlive(object)"/> rather than
    /// <see cref="System.Runtime.InteropServices.SafeHandle.DangerousAddRef"/>/<c>DangerousRelease</c>:
    /// it closes the reachability hazard, which is the real one here, but it does not make a
    /// concurrent <c>Dispose</c> from another thread safe. Nothing in this project is thread-safe,
    /// so that is consistent rather than a new gap -- and the ref-counted form is what
    /// <c>plan.md</c> WP17 will apply project-wide.
    /// </summary>
    internal CnaHandle NativeHandle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
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

    public EffectPassCollection Passes
    {
        get
        {
            CnaResult result = Native.cna_effect_technique_get_passes(NativeHandle, out CnaHandle collection);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Passes));
            return new EffectPassCollection(collection);
        }
    }

    public EffectAnnotationCollection Annotations
    {
        get
        {
            CnaResult result = Native.cna_effect_technique_get_annotations(NativeHandle, out CnaHandle collection);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            return new EffectAnnotationCollection(collection);
        }
    }
}
