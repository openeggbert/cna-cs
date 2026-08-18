using CNA.Interop;

namespace CNA.Media;

/// <summary>A music genre in a <see cref="MediaLibrary"/>. Same shape as <see cref="Artist"/>.</summary>
public class Genre : MediaLibraryObject, IEquatable<Genre>
{
    internal Genre(CnaHandle handle)
        : base(handle, Native.cna_genre_dispose, Native.cna_genre_get_is_disposed,
               h => Native.cna_genre_destroy(h))
    {
    }

    public unsafe string Name => ReadName(Native.cna_genre_get_name_size, Native.cna_genre_copy_name, nameof(Name));

    public AlbumCollection Albums =>
        ReadRequired(Native.cna_genre_get_albums, h => new AlbumCollection(h), nameof(Albums));

    public SongCollection Songs =>
        ReadRequired(Native.cna_genre_get_songs, h => new SongCollection(h), nameof(Songs));

    public bool Equals(Genre? other) => NativeEquals(Native.cna_genre_equals, other);

    public override bool Equals(object? obj) => Equals(obj as Genre);

    public override int GetHashCode() => ReadInt(Native.cna_genre_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(Genre? left, Genre? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Genre? left, Genre? right) => !(left == right);
}
