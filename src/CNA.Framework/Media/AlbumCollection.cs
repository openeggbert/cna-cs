using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Album"/> objects. Real XNA's own constructor is <c>MediaLibrary</c>-only -- kept <c>internal</c> here too. Backed by a native <c>CNA_AlbumCollectionHandle</c>; see <see cref="ReadOnlyMediaCollection{T}"/> for how elements are read and cached.</summary>
public sealed class AlbumCollection : ReadOnlyMediaCollection<Album>
{
    internal AlbumCollection(CnaHandle handle)
        : base(handle, Native.cna_album_collection_get_count, Native.cna_album_collection_get_at, h => Native.cna_album_collection_destroy(h), h => new Album(h))
    {
    }
}
