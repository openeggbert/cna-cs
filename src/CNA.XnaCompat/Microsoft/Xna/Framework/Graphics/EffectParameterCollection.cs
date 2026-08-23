using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectParameterCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. The underlying collection and facade factory both
/// preserve XNA's stable element identity.</summary>
public sealed class EffectParameterCollection : IEnumerable<EffectParameter>
{
    private readonly CNA.Graphics.EffectParameterCollection _collection;

    internal EffectParameterCollection(CNA.Graphics.EffectParameterCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectParameter this[int index]
    {
        get
        {
            CNA.Graphics.EffectParameter? element = _collection[index];
            return element is null ? null! : EffectParameter.Wrap(element);
        }
    }

    public EffectParameter? this[string name]
    {
        get
        {
            CNA.Graphics.EffectParameter? element = _collection[name];
            return element is null ? null : EffectParameter.Wrap(element);
        }
    }

    public EffectParameter? GetParameterBySemantic(string semantic)
    {
        for (int i = 0; i < _collection.Count; i++)
        {
            EffectParameter element = this[i];
            if (string.Equals(element.Semantic, semantic, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
        }

        return null;
    }

    public List<EffectParameter>.Enumerator GetEnumerator()
    {
        var parameters = new List<EffectParameter>(_collection.Count);
        for (int i = 0; i < _collection.Count; i++)
        {
            parameters.Add(this[i]);
        }

        return parameters.GetEnumerator();
    }

    IEnumerator<EffectParameter> IEnumerable<EffectParameter>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
