using System.Collections;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Genre"/> objects. Same
/// <c>internal</c>-constructor reasoning as <see cref="AlbumCollection"/>'s own doc
/// comment.</summary>
public class GenreCollection : IDisposable, IEnumerable<Genre>
{
    private readonly List<Genre> _genres;

    internal GenreCollection(IReadOnlyList<Genre> genres)
    {
        _genres = new List<Genre>(genres);
    }

    public Genre this[int index] => _genres[index];

    public int Count => _genres.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _genres.Clear();
        IsDisposed = true;
    }

    public List<Genre>.Enumerator GetEnumerator() => _genres.GetEnumerator();

    IEnumerator<Genre> IEnumerable<Genre>.GetEnumerator() => _genres.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _genres.GetEnumerator();
}
