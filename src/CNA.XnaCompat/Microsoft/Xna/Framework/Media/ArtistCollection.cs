using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>ArtistCollection</c>. Same reasoning as
/// <see cref="AlbumCollection"/>'s own doc comment.</summary>
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
