using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectTechniqueCollection</c>. Wraps the CNA.Graphics collection and
/// re-types each element on the way out -- see <see cref="EffectParameter"/> for why these
/// reflection types wrap rather than subclass. Holds no state of its own, so it cannot go stale
/// relative to the effect.</summary>
public sealed class EffectTechniqueCollection : IEnumerable<EffectTechnique>
{
    private readonly CNA.Graphics.EffectTechniqueCollection _collection;

    internal EffectTechniqueCollection(CNA.Graphics.EffectTechniqueCollection collection)
    {
        _collection = collection;
    }

    public int Count => _collection.Count;

    public EffectTechnique this[int index] => new(_collection[index]);

    public EffectTechnique? this[string name]
    {
        get
        {
            CNA.Graphics.EffectTechnique? element = _collection[name];
            return element is null ? null : new EffectTechnique(element);
        }
    }

    public List<EffectTechnique>.Enumerator GetEnumerator()
    {
        var techniques = new List<EffectTechnique>(_collection.Count);
        foreach (CNA.Graphics.EffectTechnique element in _collection)
        {
            techniques.Add(new EffectTechnique(element));
        }

        return techniques.GetEnumerator();
    }

    IEnumerator<EffectTechnique> IEnumerable<EffectTechnique>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
