using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a read-only collection of model meshes.</summary>
public sealed class ModelMeshCollection : ReadOnlyCollection<ModelMesh>
{
    private readonly IList<ModelMesh> _meshes;

    internal ModelMeshCollection(IList<ModelMesh> meshes)
        : base(meshes)
    {
        _meshes = meshes;
    }

    public ModelMesh this[string meshName]
    {
        get
        {
            if (TryGetValue(meshName, out ModelMesh? value))
            {
                return value!;
            }

            throw new KeyNotFoundException();
        }
    }

    public bool TryGetValue(string meshName, out ModelMesh? value)
    {
        if (string.IsNullOrEmpty(meshName))
        {
            throw new ArgumentNullException(nameof(meshName));
        }

        foreach (ModelMesh mesh in Items)
        {
            if (string.Equals(mesh.Name, meshName, StringComparison.Ordinal))
            {
                value = mesh;
                return true;
            }
        }

        value = null;
        return false;
    }

    public new Enumerator GetEnumerator() => new(_meshes);

    public struct Enumerator : IEnumerator<ModelMesh>
    {
        private readonly IList<ModelMesh> _items;
        private int _position;

        internal Enumerator(IList<ModelMesh> items)
        {
            _items = items;
            _position = -1;
        }

        public ModelMesh Current => _items[_position];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _position++;
            if (_position >= _items.Count)
            {
                _position = _items.Count;
                return false;
            }

            return true;
        }

        public void Dispose()
        {
        }

        void IEnumerator.Reset() => _position = -1;
    }
}
