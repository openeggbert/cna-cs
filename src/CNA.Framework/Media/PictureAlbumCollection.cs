namespace CNA.Media;

/// <summary>An ordered, read-only (from the outside) collection of <see cref="PictureAlbum"/>
/// objects. Same reasoning as <see cref="PictureCollection"/>'s own doc comment --
/// <see cref="MediaLibrary"/> grows this one too, when a new picture album (e.g. "Saved Pictures")
/// needs to be registered as a real child of an existing album.</summary>
public sealed class PictureAlbumCollection : ReadOnlyMediaCollection<PictureAlbum>
{
    internal PictureAlbumCollection(IReadOnlyList<PictureAlbum> albums)
        : base(albums)
    {
    }
}
