namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Playlist</c>. Same reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Playlist : CNA.Media.Playlist
{
    internal Playlist(string name, CNA.Media.SongCollection songs, TimeSpan duration)
        : base(name, songs, duration)
    {
    }

    public new SongCollection Songs { get; } = new([]);
}
