namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Genre"/> objects. Same
/// <c>internal</c>-constructor reasoning as <see cref="AlbumCollection"/>'s own doc
/// comment.</summary>
public sealed class GenreCollection : ReadOnlyMediaCollection<Genre>
{
    internal GenreCollection(IReadOnlyList<Genre> genres)
        : base(genres)
    {
    }
}
