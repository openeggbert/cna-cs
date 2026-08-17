namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Genre</c>. Same reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Genre : CNA.Media.Genre
{
    internal Genre(string name, CNA.Media.AlbumCollection albums, CNA.Media.SongCollection songs)
        : base(name, albums, songs)
    {
    }

    public new AlbumCollection Albums { get; } = new([]);

    public new SongCollection Songs { get; } = new([]);
}
