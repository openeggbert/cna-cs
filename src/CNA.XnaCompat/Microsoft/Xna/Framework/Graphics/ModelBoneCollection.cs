using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a read-only set of bones associated with a model.</summary>
public sealed class ModelBoneCollection : ReadOnlyCollection<ModelBone>
{
    private readonly IList<ModelBone> _bones;

    internal ModelBoneCollection(IList<ModelBone> bones)
        : base(bones)
    {
        _bones = bones;
    }

    public ModelBone this[string boneName]
    {
        get
        {
            if (TryGetValue(boneName, out ModelBone? value))
            {
                return value!;
            }

            throw new KeyNotFoundException();
        }
    }

    public bool TryGetValue(string boneName, out ModelBone? value)
    {
        if (string.IsNullOrEmpty(boneName))
        {
            throw new ArgumentNullException(nameof(boneName));
        }

        foreach (ModelBone bone in Items)
        {
            if (string.Equals(bone.Name, boneName, StringComparison.Ordinal))
            {
                value = bone;
                return true;
            }
        }

        value = null;
        return false;
    }

    public new Enumerator GetEnumerator() => new(_bones);

    public struct Enumerator : IEnumerator<ModelBone>
    {
        private readonly IList<ModelBone> _items;
        private int _position;

        internal Enumerator(IList<ModelBone> items)
        {
            _items = items;
            _position = -1;
        }

        public ModelBone Current => _items[_position];

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
