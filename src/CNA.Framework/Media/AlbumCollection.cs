using System.Collections;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Album"/> objects. Real XNA's own
/// constructor is <c>MediaLibrary</c>-only -- kept <c>internal</c> here too, same reasoning as
/// <see cref="Artist"/>'s own doc comment.</summary>
public class AlbumCollection : IDisposable, IEnumerable<Album>
{
    private readonly List<Album> _albums;

    internal AlbumCollection(IReadOnlyList<Album> albums)
    {
        _albums = new List<Album>(albums);
    }

    public Album this[int index] => _albums[index];

    public int Count => _albums.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _albums.Clear();
        IsDisposed = true;
    }

    public List<Album>.Enumerator GetEnumerator() => _albums.GetEnumerator();

    IEnumerator<Album> IEnumerable<Album>.GetEnumerator() => _albums.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _albums.GetEnumerator();
}
