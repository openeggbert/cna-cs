using CNA.Interop;

namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Playlist"/> objects. Same shape as <see cref="AlbumCollection"/>.</summary>
public sealed class PlaylistCollection : ReadOnlyMediaCollection<Playlist>
{
    internal PlaylistCollection(CnaHandle handle)
        : base(handle, Native.cna_playlist_collection_get_count, Native.cna_playlist_collection_get_at, h => Native.cna_playlist_collection_destroy(h), h => new Playlist(h))
    {
    }
}
