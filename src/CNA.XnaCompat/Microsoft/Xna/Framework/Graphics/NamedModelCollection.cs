using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Shared int/by-name indexer + <c>Count</c>/<c>TryGetValue</c>/<c>Contains</c>/enumerator
/// implementation for this namespace's <see cref="ModelBoneCollection"/>/<see cref="ModelMeshCollection"/>
/// -- a code-review finding: the two were originally near-verbatim copy-paste duplicates of each
/// other, differing only in element type, the same shape of duplication
/// <c>CNA.Media.ReadOnlyMediaCollection&lt;T&gt;</c> was extracted to eliminate elsewhere in this
/// codebase (though that type's own shape doesn't fit here -- its collections have no by-name
/// lookup at all, unlike <c>ModelBoneCollection</c>/<c>ModelMeshCollection</c>, which both do).
/// <c>public</c>, not <c>internal</c>, for the same C# CS0060 reason
/// <c>ReadOnlyMediaCollection&lt;T&gt;</c> already is: a <c>public sealed class ModelBoneCollection</c>
/// cannot derive from an <c>internal</c> base.
/// </summary>
public class NamedModelCollection<T> : IEnumerable<T> where T : class
{
    private readonly List<T> _items;
    private readonly Func<T, string> _nameSelector;
    private readonly string _elementKind;

    internal NamedModelCollection(List<T> items, Func<T, string> nameSelector, string elementKind)
    {
        _items = items;
        _nameSelector = nameSelector;
        _elementKind = elementKind;
    }

    public T this[int index] => _items[index];

    public T this[string name]
    {
        get
        {
            if (TryGetValue(name, out T? value))
            {
                return value;
            }

            throw new KeyNotFoundException($"A {_elementKind} named '{name}' was not found in this collection.");
        }
    }

    public int Count => _items.Count;

    public bool TryGetValue(string name, [NotNullWhen(true)] out T? value)
    {
        ArgumentNullException.ThrowIfNull(name);

        foreach (T item in _items)
        {
            if (_nameSelector(item) == name)
            {
                value = item;
                return true;
            }
        }

        value = null;
        return false;
    }

    public bool Contains(T item) => _items.Contains(item);

    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
