using System.Collections;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>ModelMeshPartCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Graphics.ModelMeshPartCollection</c> -- same reasoning as
/// <see cref="ModelBoneCollection"/>'s own doc comment. Index-only (no by-name lookup), matching
/// the base type's own shape exactly -- real XNA's own <c>ModelMeshPartCollection</c> has no
/// string indexer either.</summary>
public sealed class ModelMeshPartCollection : IEnumerable<ModelMeshPart>
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
