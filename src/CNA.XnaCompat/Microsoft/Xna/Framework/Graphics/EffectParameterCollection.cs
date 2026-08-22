using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectParameterCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. Holds no state of its own, so it cannot go stale
/// relative to the effect.</summary>
public sealed class EffectParameterCollection : IEnumerable<EffectParameter>
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

    public EffectParameter? GetParameterBySemantic(string semantic)
    {
        ArgumentNullException.ThrowIfNull(semantic);
        foreach (CNA.Graphics.EffectParameter element in _collection)
        {
            if (string.Equals(element.Semantic, semantic, StringComparison.Ordinal))
            {
                return new EffectParameter(element);
            }
        }

        return null;
    }

    public List<EffectParameter>.Enumerator GetEnumerator()
    {
        var parameters = new List<EffectParameter>(_collection.Count);
        foreach (CNA.Graphics.EffectParameter element in _collection)
        {
            parameters.Add(new EffectParameter(element));
        }

        return parameters.GetEnumerator();
    }

    IEnumerator<EffectParameter> IEnumerable<EffectParameter>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
