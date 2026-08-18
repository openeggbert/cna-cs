using System.Collections;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectAnnotationCollection</c>: the annotations attached to a parameter, technique or pass, reached by index or by name.
///
/// A borrowed view over a native collection the effect owns -- see <see cref="EffectParameter"/>
/// for the ownership rule. Nothing is cached: <see cref="Count"/> and the indexers each round-trip
/// to native, so the collection cannot go stale relative to the effect it belongs to.
/// </summary>
public class EffectAnnotationCollection : IEnumerable<EffectAnnotation>
{
    private readonly CnaHandle _handle;

    internal EffectAnnotationCollection(CnaHandle handle)
    {
        _handle = handle;
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
