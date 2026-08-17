namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>GenreCollection</c>. Same reasoning as
/// <see cref="AlbumCollection"/>'s own doc comment.</summary>
public sealed class GenreCollection : ReadOnlyMediaCollection<Genre>
{
    internal GenreCollection(IReadOnlyList<Genre> genres)
        : base(genres)
    {
    }
}
