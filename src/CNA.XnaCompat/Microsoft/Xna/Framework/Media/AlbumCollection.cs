using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>AlbumCollection</c>. Independent implementation, same
/// reasoning as <see cref="SongCollection"/>'s own doc comment. <c>internal</c> constructor,
/// matching real XNA's own <c>MediaLibrary</c>-only construction (unlike <see cref="SongCollection"/>,
/// which needs a public one for <c>MediaPlayer.Play</c>).</summary>
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
