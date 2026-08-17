namespace CNA.Media;

/// <summary>
/// An ordered, read-only collection of <see cref="Song"/>s, used with <see cref="MediaPlayer.Play(SongCollection)"/>.
/// Real XNA's own constructor is content-pipeline-only (no public XNA constructor produces one
/// directly); this project has none, so, matching the real openeggbert/cna C++ engine's own
/// <c>CNAEXT</c>-marked constructor, this one is public -- same "the only construction path that
/// actually exists here" reasoning as <see cref="Song"/>'s own constructor.
/// </summary>
public sealed class SongCollection : ReadOnlyMediaCollection<Song>
{
    public SongCollection(IReadOnlyList<Song> songs)
        : base(songs)
    {
    }
}
