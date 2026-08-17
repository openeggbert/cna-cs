using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>ModelMeshCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Graphics.ModelMeshCollection</c> -- same reasoning as
/// <see cref="ModelBoneCollection"/>'s own doc comment.</summary>
public sealed class ModelMeshCollection : IEnumerable<ModelMesh>
{
    private readonly List<ModelMesh> _meshes;

    internal ModelMeshCollection(List<ModelMesh> meshes)
    {
        _meshes = meshes;
    }

    public ModelMesh this[int index] => _meshes[index];

    public ModelMesh this[string name]
    {
        get
        {
            if (TryGetValue(name, out ModelMesh? mesh))
            {
                return mesh;
            }

            throw new KeyNotFoundException($"A mesh named '{name}' was not found in this collection.");
        }
    }

    public int Count => _meshes.Count;

    public bool TryGetValue(string meshName, [NotNullWhen(true)] out ModelMesh? value)
    {
        ArgumentNullException.ThrowIfNull(meshName);

        foreach (ModelMesh mesh in _meshes)
        {
            if (mesh.Name == meshName)
            {
                value = mesh;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool Contains(ModelMesh item) => _meshes.Contains(item);

    public List<ModelMesh>.Enumerator GetEnumerator() => _meshes.GetEnumerator();

    IEnumerator<ModelMesh> IEnumerable<ModelMesh>.GetEnumerator() => _meshes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _meshes.GetEnumerator();
}
