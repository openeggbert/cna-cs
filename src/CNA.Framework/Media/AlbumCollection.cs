namespace CNA.Media;

/// <summary>An ordered, read-only collection of <see cref="Album"/> objects. Real XNA's own
/// constructor is <c>MediaLibrary</c>-only -- kept <c>internal</c> here too, same reasoning as
/// <see cref="Artist"/>'s own doc comment.</summary>
public sealed class AlbumCollection : ReadOnlyMediaCollection<Album>
{
    internal AlbumCollection(IReadOnlyList<Album> albums)
        : base(albums)
    {
    }
}
