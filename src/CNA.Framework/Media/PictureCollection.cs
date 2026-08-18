using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Picture"/> objects. Same shape as <see cref="AlbumCollection"/>. Reads through to native on every access, so a picture saved after this collection was obtained is visible without rebuilding it.</summary>
public sealed class PictureCollection : ReadOnlyMediaCollection<Picture>
{
    internal PictureCollection(CnaHandle handle)
        : base(handle, Native.cna_picture_collection_get_count, Native.cna_picture_collection_get_at, h => Native.cna_picture_collection_destroy(h), h => new Picture(h))
    {
    }
}
