namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PlaylistCollection</c>. Same reasoning as
/// <see cref="AlbumCollection"/>'s own doc comment.</summary>
public sealed class PlaylistCollection : ReadOnlyMediaCollection<Playlist>
{
    internal PlaylistCollection(IReadOnlyList<Playlist> playlists)
        : base(playlists)
    {
    }
}
