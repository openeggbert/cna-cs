namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Playlist</c>. Same reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Playlist : CNA.Media.Playlist
{
    internal Playlist(string name, SongCollection songs, TimeSpan duration)
        : base(name, ToBaseSongs(songs), duration)
    {
        Songs = songs;
    }

    public new SongCollection Songs { get; }

    private static CNA.Media.SongCollection ToBaseSongs(SongCollection songs) => new(songs.ToList());
}
