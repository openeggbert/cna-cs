namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Artist</c>. Extends <c>CNA.Media.Artist</c> directly.
/// <c>Equals</c>/<c>GetHashCode</c>/<c>ToString</c>/<c>==</c>/<c>!=</c> are inherited unchanged --
/// all name-based, no compat-type crossing needed, same "only override what actually needs a
/// different type" precedent <c>Song</c> already established. Only <see cref="Albums"/> needs a
/// <c>new</c> override, and it's always an empty collection either way -- see
/// <c>CNA.Media.MediaLibrary</c>'s own doc comment for why this feature is scoped this
/// way.</summary>
public class Artist : CNA.Media.Artist
{
    internal Artist(string name, CNA.Media.AlbumCollection albums, CNA.Media.SongCollection songs)
        : base(name, albums, songs)
    {
    }

    public new AlbumCollection Albums { get; } = new([]);

    public new SongCollection Songs { get; } = new([]);
}
