using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible read-only view of the effects used by a model mesh.</summary>
public sealed class ModelEffectCollection : ReadOnlyCollection<Effect>
{
    internal ModelEffectCollection(CNA.Graphics.ModelEffectCollection effects)
        : base(new EffectListAdapter(effects))
    {
    }

    public new Enumerator GetEnumerator() => new(Items);

    public struct Enumerator : IEnumerator<Effect>
    {
        private readonly IList<Effect> _items;
        private int _index;

        internal Enumerator(IList<Effect> items)
        {
            _items = items;
            _index = -1;
        }

        public Effect Current => _items[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index + 1 >= _items.Count)
            {
                _index = _items.Count;
                return false;
            }

            _index++;
            return true;
        }

        public void Dispose()
        {
        }

        void IEnumerator.Reset() => _index = -1;
    }

    private sealed class EffectListAdapter : IList<Effect>
    {
        private readonly CNA.Graphics.ModelEffectCollection _effects;

        internal EffectListAdapter(CNA.Graphics.ModelEffectCollection effects)
        {
            _effects = effects;
        }

        public Effect this[int index]
        {
            get => Effect.FromFramework(_effects[index])!;
            set => throw new NotSupportedException();
        }

        public int Count => _effects.Count;

        public bool IsReadOnly => true;

        public void Add(Effect item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(Effect item) => _effects.Contains(item.Inner);

        public void CopyTo(Effect[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public IEnumerator<Effect> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        public int IndexOf(Effect item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (ReferenceEquals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, Effect item) => throw new NotSupportedException();

        public bool Remove(Effect item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
