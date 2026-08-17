using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>SongCollection</c>. An independent implementation, not a subclass of
/// <c>CNA.Media.SongCollection</c> -- extending it directly would inherit an indexer/enumerator
/// typed to <c>CNA.Media.Song</c>, not this namespace's own <see cref="Song"/>, defeating the
/// point of a compat-typed collection. Same shape as <c>VertexDeclaration</c>'s own
/// wrap-don't-subclass choice, just duplicated rather than composed since there's no underlying
/// native/framework instance to wrap here.
/// </summary>
public class SongCollection : IDisposable, IEnumerable<Song>
{
    private readonly List<Song> _songs;

    public SongCollection(IReadOnlyList<Song> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);

        _songs = new List<Song>(songs);
    }

    public Song this[int index] => _songs[index];

    public int Count => _songs.Count;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        _songs.Clear();
        IsDisposed = true;
    }

    public List<Song>.Enumerator GetEnumerator() => _songs.GetEnumerator();

    IEnumerator<Song> IEnumerable<Song>.GetEnumerator() => _songs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _songs.GetEnumerator();
}
