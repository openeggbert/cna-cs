using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a read-only collection of effects associated with a model mesh.</summary>
public sealed class ModelEffectCollection : ReadOnlyCollection<Effect>
{
    private readonly List<Effect> _effects;

    internal ModelEffectCollection()
        : this([])
    {
    }

    private ModelEffectCollection(List<Effect> effects)
        : base(effects)
    {
        _effects = effects;
    }

    internal void Add(Effect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        Items.Add(effect);
    }

    internal void Remove(Effect effect) => Items.Remove(effect);

    public new Enumerator GetEnumerator() => new(_effects);

    public struct Enumerator : IEnumerator<Effect>
    {
        private List<Effect>.Enumerator _internalEnumerator;

        internal Enumerator(List<Effect> effects)
        {
            _internalEnumerator = effects.GetEnumerator();
        }

        public Effect Current => _internalEnumerator.Current;

        object IEnumerator.Current => Current;

        public bool MoveNext() => _internalEnumerator.MoveNext();

        public void Dispose() => _internalEnumerator.Dispose();

        void IEnumerator.Reset()
        {
            IEnumerator enumerator = _internalEnumerator;
            enumerator.Reset();
            _internalEnumerator = (List<Effect>.Enumerator)enumerator;
        }
    }
}
