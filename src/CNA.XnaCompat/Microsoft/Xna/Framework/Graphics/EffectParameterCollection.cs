using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectParameterCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. Holds no state of its own, so it cannot go stale
/// relative to the effect.</summary>
public class EffectParameterCollection : IEnumerable<EffectParameter>
{
    private readonly CNA.Graphics.EffectParameterCollection _collection;

    internal EffectParameterCollection(CNA.Graphics.EffectParameterCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectParameter this[int index] => new(_collection[index]);

    public EffectParameter? this[string name]
    {
        get
        {
            CNA.Graphics.EffectParameter? element = _collection[name];
            return element is null ? null : new EffectParameter(element);
        }
    }

    public IEnumerator<EffectParameter> GetEnumerator()
    {
        foreach (CNA.Graphics.EffectParameter element in _collection)
        {
            yield return new EffectParameter(element);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
