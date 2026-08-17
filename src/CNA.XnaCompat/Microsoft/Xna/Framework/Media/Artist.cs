namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Artist</c>. Extends <c>CNA.Media.Artist</c> directly.
/// <c>Equals</c>/<c>GetHashCode</c>/<c>ToString</c>/<c>==</c>/<c>!=</c> are inherited unchanged --
/// all name-based, no compat-type crossing needed, same "only override what actually needs a
/// different type" precedent <c>Song</c> already established. <see cref="Albums"/>/<see cref="Songs"/>
/// need <c>new</c> overrides; the constructor takes compat-typed collections directly (not
/// <c>CNA.Media</c>-namespaced ones) so those overrides genuinely reflect what was passed rather
/// than silently discarding it -- a real code-review finding, fixed here rather than left as a
/// documented gap.</summary>
public class Artist : CNA.Media.Artist
{
    internal Artist(string name, AlbumCollection albums, SongCollection songs)
        : base(name, ToBaseAlbums(albums), ToBaseSongs(songs))
    {
        Albums = albums;
        Songs = songs;
    }

    public new AlbumCollection Albums { get; }

    public new SongCollection Songs { get; }

    private static CNA.Media.AlbumCollection ToBaseAlbums(AlbumCollection albums) => new(albums.ToList());

    private static CNA.Media.SongCollection ToBaseSongs(SongCollection songs) => new(songs.ToList());
}
