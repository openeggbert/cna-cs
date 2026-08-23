using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectTechniqueCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. The underlying collection and facade factory both
/// preserve XNA's stable element identity.</summary>
public sealed class EffectTechniqueCollection : IEnumerable<EffectTechnique>
{
    private readonly CNA.Graphics.EffectTechniqueCollection _collection;

    internal EffectTechniqueCollection(CNA.Graphics.EffectTechniqueCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectTechnique this[int index]
    {
        get
        {
            CNA.Graphics.EffectTechnique? element = _collection[index];
            return element is null ? null! : EffectTechnique.Wrap(element);
        }
    }

    public EffectTechnique? this[string name]
    {
        get
        {
            CNA.Graphics.EffectTechnique? element = _collection[name];
            return element is null ? null : EffectTechnique.Wrap(element);
        }
    }

    public List<EffectTechnique>.Enumerator GetEnumerator()
    {
        var techniques = new List<EffectTechnique>(_collection.Count);
        for (int i = 0; i < _collection.Count; i++)
        {
            techniques.Add(this[i]);
        }

        return techniques.GetEnumerator();
    }

    IEnumerator<EffectTechnique> IEnumerable<EffectTechnique>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
