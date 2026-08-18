using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectAnnotationCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. Holds no state of its own, so it cannot go stale
/// relative to the effect.</summary>
public class EffectAnnotationCollection : IEnumerable<EffectAnnotation>
{
    private readonly CNA.Graphics.EffectAnnotationCollection _collection;

    internal EffectAnnotationCollection(CNA.Graphics.EffectAnnotationCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectAnnotation this[int index] => new(_collection[index]);

    public EffectAnnotation? this[string name]
    {
        get
        {
            CNA.Graphics.EffectAnnotation? element = _collection[name];
            return element is null ? null : new EffectAnnotation(element);
        }
    }

    public IEnumerator<EffectAnnotation> GetEnumerator()
    {
        foreach (CNA.Graphics.EffectAnnotation element in _collection)
        {
            yield return new EffectAnnotation(element);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
