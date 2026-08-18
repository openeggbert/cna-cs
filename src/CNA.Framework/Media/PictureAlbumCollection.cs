using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="PictureAlbum"/> objects. Same shape as <see cref="AlbumCollection"/>.</summary>
public sealed class PictureAlbumCollection : ReadOnlyMediaCollection<PictureAlbum>
{
    internal PictureAlbumCollection(CnaHandle handle)
        : base(handle, Native.cna_picture_album_collection_get_count, Native.cna_picture_album_collection_get_at, h => Native.cna_picture_album_collection_destroy(h), h => new PictureAlbum(h))
    {
    }
}
