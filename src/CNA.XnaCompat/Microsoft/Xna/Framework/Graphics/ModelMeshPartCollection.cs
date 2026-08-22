using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a read-only collection of model mesh parts.</summary>
public sealed class ModelMeshPartCollection : ReadOnlyCollection<ModelMeshPart>
{
    private readonly IList<ModelMeshPart> _parts;

    internal ModelMeshPartCollection(IList<ModelMeshPart> parts)
        : base(parts)
    {
        _parts = parts;
    }

    public new Enumerator GetEnumerator() => new(_parts);

    public struct Enumerator : IEnumerator<ModelMeshPart>
    {
        private readonly IList<ModelMeshPart> _items;
        private int _position;

        internal Enumerator(IList<ModelMeshPart> items)
        {
            _items = items;
            _position = -1;
        }

        public ModelMeshPart Current => _items[_position];

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
