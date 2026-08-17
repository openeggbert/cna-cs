namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Artist"/> objects. Same
/// <c>internal</c>-constructor reasoning as <see cref="AlbumCollection"/>'s own doc
/// comment.</summary>
public sealed class ArtistCollection : ReadOnlyMediaCollection<Artist>
{
    internal ArtistCollection(IReadOnlyList<Artist> artists)
        : base(artists)
    {
    }
}
