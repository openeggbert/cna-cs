namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Playlist"/> objects. Same
/// <c>internal</c>-constructor reasoning as <see cref="AlbumCollection"/>'s own doc
/// comment.</summary>
public sealed class PlaylistCollection : ReadOnlyMediaCollection<Playlist>
{
    internal PlaylistCollection(IReadOnlyList<Playlist> playlists)
        : base(playlists)
    {
    }
}
