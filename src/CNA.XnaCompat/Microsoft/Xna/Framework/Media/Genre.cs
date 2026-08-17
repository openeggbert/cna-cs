namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Genre</c>. Same reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Genre : CNA.Media.Genre
{
    internal Genre(string name, AlbumCollection albums, SongCollection songs)
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
