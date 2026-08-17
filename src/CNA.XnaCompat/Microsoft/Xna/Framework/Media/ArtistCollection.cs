namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>ArtistCollection</c>. Same reasoning as
/// <see cref="AlbumCollection"/>'s own doc comment.</summary>
public sealed class ArtistCollection : ReadOnlyMediaCollection<Artist>
{
    internal ArtistCollection(IReadOnlyList<Artist> artists)
        : base(artists)
    {
    }
}
