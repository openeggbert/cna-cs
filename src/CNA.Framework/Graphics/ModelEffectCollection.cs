using System.Collections;

namespace CNA.Graphics;

/// <summary>
/// Real XNA's <c>ModelEffectCollection</c> -- the set of distinct <see cref="Effect"/>s a
/// <see cref="ModelMesh"/>'s parts use, auto-maintained by <see cref="ModelMeshPart.Effect"/>'s
/// setter (see that property's own doc comment). Real XNA's own <c>Add</c>/<c>Remove</c> are
/// content-pipeline-only (<c>internal</c>); the real openeggbert/cna C++ engine deliberately marks
/// them <c>CNAEXT</c> public instead, for the same "no content pipeline exists here, so this is
/// the only construction/maintenance path available" reason <see cref="ModelBone.AddChild"/> is
/// public -- reproduced here rather than trying to hide them and then immediately needing an
/// internal-only escape hatch for <see cref="ModelMeshPart.Effect"/> to use anyway.
/// </summary>
public class ModelEffectCollection : IEnumerable<Effect>
{
    private readonly List<Effect> _effects = [];

    public Effect this[int index] => _effects[index];

    public int Count => _effects.Count;

    public bool Contains(Effect effect) => _effects.Contains(effect);

    public void Add(Effect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects.Add(effect);
    }

    public void Remove(Effect effect) => _effects.Remove(effect);

    public List<Effect>.Enumerator GetEnumerator() => _effects.GetEnumerator();

    IEnumerator<Effect> IEnumerable<Effect>.GetEnumerator() => _effects.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _effects.GetEnumerator();
}
