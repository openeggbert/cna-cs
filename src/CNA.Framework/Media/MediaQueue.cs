using System.Collections;

namespace CNA.Media;

/// <summary>
/// The ordered list of songs <see cref="MediaPlayer"/> is currently playing through, exposed via
/// <see cref="MediaPlayer.Queue"/>. Real XNA's own <c>MediaQueue</c> has no public constructor and
/// no public <c>Add</c>/<c>Clear</c> (both <c>internal</c>, mutated only by <c>MediaPlayer</c>
/// itself) -- unlike most of this session's other real-XNA-internal-construction cases, this one
/// needed no <c>CNAEXT</c> deviation to stay usable: nothing outside <c>MediaPlayer</c> ever needs
/// to build a <see cref="MediaQueue"/> from scratch, since it's always populated by
/// <see cref="MediaPlayer.Play(Song)"/>/<see cref="MediaPlayer.Play(SongCollection,int)"/>, so this
/// type keeps that real-XNA encapsulation exactly.
/// </summary>
public class MediaQueue : IEnumerable<Song>
{
    private readonly List<Song> _songs = [];

    internal MediaQueue()
    {
        // Matches the real C++ engine's own MediaQueue constructor: -1, not 0, so an empty queue's
        // ActiveSong correctly reports null rather than accidentally indexing a would-be entry 0
        // that doesn't exist.
        ActiveSongIndex = -1;
    }

    public Song? ActiveSong =>
        _songs.Count == 0 || ActiveSongIndex < 0 || ActiveSongIndex >= _songs.Count
            ? null
            : _songs[ActiveSongIndex];

    public int ActiveSongIndex { get; internal set; }

    public int Count => _songs.Count;

    public Song this[int index] => _songs[index];

    internal void Add(Song song) => _songs.Add(song);

    internal void Clear()
    {
        _songs.Clear();
        ActiveSongIndex = -1;
    }

    public List<Song>.Enumerator GetEnumerator() => _songs.GetEnumerator();

    IEnumerator<Song> IEnumerable<Song>.GetEnumerator() => _songs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _songs.GetEnumerator();
}
