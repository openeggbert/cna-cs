using System.Collections;

namespace CNA.Media;

/// <summary>
/// Shared indexer/<c>Count</c>/<c>Dispose</c>/enumerator implementation for this namespace's
/// several simple read-only collections (<see cref="SongCollection"/>/<see cref="AlbumCollection"/>/
/// <see cref="ArtistCollection"/>/<see cref="GenreCollection"/>/<see cref="PlaylistCollection"/>).
/// Extracted after a code review flagged the same ~30 lines duplicated five times with no shared
/// base -- exactly the kind of small, mechanical, no-real-variation duplication this project's own
/// <c>BufferRangeValidation</c> precedent already established is worth extracting once it recurs
/// this many times. Necessarily <c>public</c> (a <c>public sealed class SongCollection</c> cannot
/// derive from an <c>internal</c> base -- C# CS0060), in the same spirit as the BCL's own
/// <c>System.Collections.ObjectModel.ReadOnlyCollection&lt;T&gt;</c>: the named collection types
/// above are the real public API surface real XNA specifies, and this is their shared
/// implementation detail, not something a caller would typically reference directly, but there's
/// no harm if one does.
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

    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
