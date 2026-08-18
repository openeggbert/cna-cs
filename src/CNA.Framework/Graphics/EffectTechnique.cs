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

    internal CnaHandle NativeHandle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_effect_technique_get_name_byte_count, Native.cna_effect_technique_copy_name, NativeHandle, nameof(Name));

    public EffectPassCollection Passes
    {
        get
        {
            CnaResult result = Native.cna_effect_technique_get_passes(NativeHandle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Passes));
            return new EffectPassCollection(collection);
        }
    }

    public EffectAnnotationCollection Annotations
    {
        get
        {
            CnaResult result = Native.cna_effect_technique_get_annotations(NativeHandle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            return new EffectAnnotationCollection(collection);
        }
    }
}
