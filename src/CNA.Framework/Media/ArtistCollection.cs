using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Artist"/> objects. Same shape as <see cref="AlbumCollection"/>.</summary>
public sealed class ArtistCollection : ReadOnlyMediaCollection<Artist>
{
    internal ArtistCollection(CnaHandle handle)
        : base(handle, Native.cna_artist_collection_get_count, Native.cna_artist_collection_get_at, h => Native.cna_artist_collection_destroy(h), h => new Artist(h))
    {
    }
}
