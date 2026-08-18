using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>EffectPass</c>. Native-backed since Phase 8 WP4a: <see cref="Apply"/>
/// calls <c>cna_effect_pass_apply</c> for this specific pass, where previously it forwarded to the
/// whole effect's <c>Apply()</c> because no per-pass native object was bound -- which made a
/// multi-pass technique silently render only whatever the effect's single apply did.</summary>
public class EffectPass
{
    private readonly CnaHandle _handle;

    internal EffectPass(CnaHandle handle)
    {
        _handle = handle;
    }

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_effect_pass_get_name_byte_count, Native.cna_effect_pass_copy_name, _handle, nameof(Name));

    public EffectAnnotationCollection Annotations
    {
        get
        {
            CnaResult result = Native.cna_effect_pass_get_annotations(_handle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            return new EffectAnnotationCollection(collection);
        }
    }

    public void Apply()
    {
        CnaResult result = Native.cna_effect_pass_apply(_handle);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }
}
