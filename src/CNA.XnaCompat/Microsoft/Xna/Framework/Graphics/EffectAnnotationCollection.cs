using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectAnnotationCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. The underlying collection and facade factory both
/// preserve XNA's stable element identity.</summary>
public sealed class EffectAnnotationCollection : IEnumerable<EffectAnnotation>
{
    private readonly CNA.Graphics.EffectAnnotationCollection _collection;

    internal EffectAnnotationCollection(CNA.Graphics.EffectAnnotationCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectAnnotation this[int index]
    {
        get
        {
            CNA.Graphics.EffectAnnotation? element = _collection[index];
            return element is null ? null! : EffectAnnotation.Wrap(element);
        }
    }

    public EffectAnnotation? this[string name]
    {
        get
        {
            CNA.Graphics.EffectAnnotation? element = _collection[name];
            return element is null ? null : EffectAnnotation.Wrap(element);
        }
    }

    public List<EffectAnnotation>.Enumerator GetEnumerator()
    {
        var annotations = new List<EffectAnnotation>(_collection.Count);
        for (int i = 0; i < _collection.Count; i++)
        {
            annotations.Add(this[i]);
        }

        return annotations.GetEnumerator();
    }

    IEnumerator<EffectAnnotation> IEnumerable<EffectAnnotation>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
