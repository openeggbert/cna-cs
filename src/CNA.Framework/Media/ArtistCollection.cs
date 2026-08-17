using System.Collections;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Artist"/> objects. Same
/// <c>internal</c>-constructor reasoning as <see cref="AlbumCollection"/>'s own doc
/// comment.</summary>
public class ArtistCollection : IDisposable, IEnumerable<Artist>
{
    private readonly List<Artist> _artists;

    internal ArtistCollection(IReadOnlyList<Artist> artists)
    {
        _artists = new List<Artist>(artists);
    }

    public Artist this[int index] => _artists[index];

    public int Count => _artists.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _artists.Clear();
        IsDisposed = true;
    }

    public List<Artist>.Enumerator GetEnumerator() => _artists.GetEnumerator();

    IEnumerator<Artist> IEnumerable<Artist>.GetEnumerator() => _artists.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _artists.GetEnumerator();
}
