namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureAlbumCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Media.PictureAlbumCollection</c> -- same reasoning as <see cref="SongCollection"/>'s
/// own doc comment.</summary>
public sealed class PictureAlbumCollection : ReadOnlyMediaCollection<PictureAlbum>
{
    internal PictureAlbumCollection(IReadOnlyList<PictureAlbum> albums)
        : base(albums)
    {
    }
}
