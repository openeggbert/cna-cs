using System.Collections;

namespace CNA.Graphics;

public class EffectPassCollection : IEnumerable<EffectPass>
{
    private readonly EffectPass[] _passes;

    internal EffectPassCollection(EffectPass pass)
    {
        _passes = [pass];
    }

    public int Count => _passes.Length;

    public EffectPass this[int index] => _passes[index];

    public IEnumerator<EffectPass> GetEnumerator() => ((IEnumerable<EffectPass>)_passes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
