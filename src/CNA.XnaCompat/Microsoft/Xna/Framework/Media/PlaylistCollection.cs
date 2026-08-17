using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PlaylistCollection</c>. Same reasoning as
/// <see cref="AlbumCollection"/>'s own doc comment.</summary>
public class PlaylistCollection : IDisposable, IEnumerable<Playlist>
{
    private readonly List<Playlist> _playlists;

    internal PlaylistCollection(IReadOnlyList<Playlist> playlists)
    {
        _playlists = new List<Playlist>(playlists);
    }

    public Playlist this[int index] => _playlists[index];

    public int Count => _playlists.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _playlists.Clear();
        IsDisposed = true;
    }

    public List<Playlist>.Enumerator GetEnumerator() => _playlists.GetEnumerator();

    IEnumerator<Playlist> IEnumerable<Playlist>.GetEnumerator() => _playlists.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _playlists.GetEnumerator();
}
