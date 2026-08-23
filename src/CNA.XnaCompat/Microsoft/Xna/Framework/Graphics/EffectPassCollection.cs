using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectPassCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. The underlying collection and facade factory both
/// preserve XNA's stable element identity.</summary>
public sealed class EffectPassCollection : IEnumerable<EffectPass>
{
    private readonly CNA.Graphics.EffectPassCollection _collection;

    internal EffectPassCollection(CNA.Graphics.EffectPassCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectPass this[int index]
    {
        get
        {
            CNA.Graphics.EffectPass? element = _collection[index];
            return element is null ? null! : EffectPass.Wrap(element);
        }
    }

    public EffectPass? this[string name]
    {
        get
        {
            CNA.Graphics.EffectPass? element = _collection[name];
            return element is null ? null : EffectPass.Wrap(element);
        }
    }

    public List<EffectPass>.Enumerator GetEnumerator()
    {
        var passes = new List<EffectPass>(_collection.Count);
        for (int i = 0; i < _collection.Count; i++)
        {
            passes.Add(this[i]);
        }

        return passes.GetEnumerator();
    }

    IEnumerator<EffectPass> IEnumerable<EffectPass>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
