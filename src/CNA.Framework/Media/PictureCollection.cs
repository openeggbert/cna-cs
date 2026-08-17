namespace CNA.Media;

/// <summary>An ordered, read-only (from the outside) collection of <see cref="Picture"/> objects.
/// Real XNA's own constructor is <c>MediaLibrary</c>-only -- kept <c>internal</c> here too, same
/// reasoning as <see cref="AlbumCollection"/>'s own doc comment. Unlike
/// <see cref="AlbumCollection"/>/<see cref="ArtistCollection"/>/<see cref="GenreCollection"/>/
/// <see cref="PlaylistCollection"/>, this one genuinely grows after construction --
/// <see cref="MediaLibrary.SavePicture(string,byte[])"/> appends to it via
/// <see cref="ReadOnlyMediaCollection{T}.Add"/>.</summary>
public sealed class PictureCollection : ReadOnlyMediaCollection<Picture>
{
    internal PictureCollection(IReadOnlyList<Picture> pictures)
        : base(pictures)
    {
    }
}
