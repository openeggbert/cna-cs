using CNA;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>EffectTechnique</c>. Native-backed since Phase 8 WP4a -- its
/// <see cref="Name"/> and <see cref="Passes"/> are the effect's real ones, where previously this
/// type reported a hardcoded <c>"Default"</c> and exactly one fabricated pass. A borrowed handle,
/// same ownership rule as <see cref="EffectParameter"/>.</summary>
public class EffectTechnique
{
    internal EffectTechnique(CnaHandle handle)
    {
        NativeHandle = handle;
    }

    internal CnaHandle NativeHandle { get; }

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
