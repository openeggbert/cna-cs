using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectPassCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. Holds no state of its own, so it cannot go stale
/// relative to the effect.</summary>
public class EffectPassCollection : IEnumerable<EffectPass>
{
    private readonly CNA.Graphics.EffectPassCollection _collection;

    internal EffectPassCollection(CNA.Graphics.EffectPassCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectPass this[int index] => new(_collection[index]);

    public EffectPass? this[string name]
    {
        get
        {
            CNA.Graphics.EffectPass? element = _collection[name];
            return element is null ? null : new EffectPass(element);
        }
    }

    public IEnumerator<EffectPass> GetEnumerator()
    {
        foreach (CNA.Graphics.EffectPass element in _collection)
        {
            yield return new EffectPass(element);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
