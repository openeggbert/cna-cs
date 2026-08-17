using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Shared indexer/<c>Count</c>/<c>Dispose</c>/enumerator implementation for this namespace's
/// several simple read-only collections. A separate copy from <c>CNA.Media.ReadOnlyMediaCollection</c>
/// (different assembly, and these collections are independent implementations rather than
/// subclasses of their <c>CNA.Media</c> counterparts -- see <see cref="SongCollection"/>'s own doc
/// comment for why extending directly isn't an option here). <c>internal</c>, same reasoning as
/// the base layer's own -- necessarily <c>public</c> for the same C# CS0060 reason (a
/// <c>public sealed class SongCollection</c> cannot derive from an <c>internal</c> base).
/// </summary>
public class ReadOnlyMediaCollection<T> : IDisposable, IEnumerable<T>
{
    private readonly List<T> _items;

    public ReadOnlyMediaCollection(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items = new List<T>(items);
    }

    public T this[int index] => _items[index];

    public int Count => _items.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _items.Clear();
        IsDisposed = true;
    }

    /// <summary>Appends an item after construction -- <c>internal</c>, used only by this
    /// namespace's own <c>MediaLibrary</c>/<c>PictureAlbum</c> to grow their picture-related
    /// collections in place. Same rationale as <c>CNA.Media.ReadOnlyMediaCollection&lt;T&gt;</c>'s
    /// own copy of this method.</summary>
    internal void Add(T item) => _items.Add(item);

    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
