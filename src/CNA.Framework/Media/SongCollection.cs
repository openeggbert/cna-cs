using System.Collections;

namespace CNA.Media;

/// <summary>
/// An ordered, read-only collection of <see cref="Song"/>s, used with <see cref="MediaPlayer.Play(SongCollection)"/>.
/// Real XNA's own constructor is content-pipeline-only (no public XNA constructor produces one
/// directly); this project has none, so, matching the real openeggbert/cna C++ engine's own
/// <c>CNAEXT</c>-marked constructor, this one is public -- same "the only construction path that
/// actually exists here" reasoning as <see cref="Song"/>'s own constructor.
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
