using System.Collections;

namespace CNA.Graphics;

/// <summary>
/// Real XNA's <c>ModelMeshPartCollection</c> -- index-only (no name lookup, unlike
/// <see cref="ModelBoneCollection"/>/<see cref="ModelMeshCollection"/>), confirmed against the
/// real openeggbert/cna C++ engine's own <c>ModelMeshPartCollection</c>, which has no string
/// indexer either.
/// </summary>
public class ModelMeshPartCollection : IEnumerable<ModelMeshPart>
{
    private readonly List<ModelMeshPart> _parts;

    internal ModelMeshPartCollection(List<ModelMeshPart> parts)
    {
        _parts = parts;
    }

    public ModelMeshPart this[int index] => _parts[index];

    public int Count => _parts.Count;

    public List<ModelMeshPart>.Enumerator GetEnumerator() => _parts.GetEnumerator();

    IEnumerator<ModelMeshPart> IEnumerable<ModelMeshPart>.GetEnumerator() => _parts.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _parts.GetEnumerator();
}
