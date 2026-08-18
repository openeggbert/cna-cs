using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Genre"/> objects. Same shape as <see cref="AlbumCollection"/>.</summary>
public sealed class GenreCollection : ReadOnlyMediaCollection<Genre>
{
    internal GenreCollection(CnaHandle handle)
        : base(handle, Native.cna_genre_collection_get_count, Native.cna_genre_collection_get_at, h => Native.cna_genre_collection_destroy(h), h => new Genre(h))
    {
    }
}
