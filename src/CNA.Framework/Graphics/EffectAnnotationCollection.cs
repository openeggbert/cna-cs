using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectAnnotationCollection</c>: the annotations attached to a parameter, technique or pass, reached by index or by name.
///
/// An OWNED native collection view -- <c>effects.h</c> mints a fresh one per call rather than
/// aliasing something the effect owns, so this releases it (see <see cref="EffectParameter"/>).
/// Nothing is cached: <see cref="Count"/> and the indexers each round-trip
/// to native, so the collection cannot go stale relative to the effect it belongs to.
/// </summary>
public class EffectAnnotationCollection : IEnumerable<EffectAnnotation>, IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectAnnotationCollection(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_annotation_collection_destroy(new CnaHandle(h)));
    }

    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    /// <summary>See the element type's own doc comment: this collection view is an owned native
    /// handle, released by its SafeHandle whether or not a caller disposes it.</summary>
    public void Dispose()
    {
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public int Count
    {
        get
        {
            CnaResult result = Native.cna_effect_annotation_collection_get_count(_handle, out ulong count);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return (int)count;
        }
    }

    public EffectAnnotation this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            CnaResult result = Native.cna_effect_annotation_collection_get_at(_handle, (ulong)index, out CnaHandle element);
            CnaException.ThrowIfFailed(result, nameof(EffectAnnotationCollection));
            return new EffectAnnotation(element);
        }
    }

    /// <summary>Returns <see langword="null"/> when no entry has that name, matching real XNA --
    /// which is why callers written against XNA null-check rather than catching.</summary>
    public EffectAnnotation? this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            CnaHandle element = default;
            byte found = 0;
            CnaResult result = CnaStringMarshal.WithStringView(
                name, view => Native.cna_effect_annotation_collection_find(_handle, view, out found, out element));
            CnaException.ThrowIfFailed(result, nameof(EffectAnnotationCollection));

            return found != 0 ? new EffectAnnotation(element) : null;
        }
    }

    public IEnumerator<EffectAnnotation> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
