namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Genre</c>. Same reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Genre : CNA.Media.Genre
{
    internal Genre(string name, AlbumCollection albums, SongCollection songs)
        : base(name, MediaCollectionConversion.ToBase(albums), MediaCollectionConversion.ToBase(songs))
    {
        Albums = albums;
        Songs = songs;
    }

    public new AlbumCollection Albums { get; }

    public new SongCollection Songs { get; }
}
