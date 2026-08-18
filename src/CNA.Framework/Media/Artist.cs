using CNA.Interop;

namespace CNA.Media;

/// <summary>A music artist in a <see cref="MediaLibrary"/>. Construction is <c>internal</c>, and
/// every member a live native round trip -- see <see cref="Album"/> for both.</summary>
public class Artist : MediaLibraryObject, IEquatable<Artist>
{
    internal Artist(CnaHandle handle)
        : base(handle, Native.cna_artist_dispose, Native.cna_artist_get_is_disposed,
               h => Native.cna_artist_destroy(h))
    {
    }

    public unsafe string Name => ReadName(Native.cna_artist_get_name_size, Native.cna_artist_copy_name, nameof(Name));

    public AlbumCollection Albums =>
        ReadRequired(Native.cna_artist_get_albums, h => new AlbumCollection(h), nameof(Albums));

    public SongCollection Songs =>
        ReadRequired(Native.cna_artist_get_songs, h => new SongCollection(h), nameof(Songs));

    /// <summary>By name -- delegated to <c>cna_artist_equals</c>, see
    /// <see cref="MediaLibraryObject"/>.</summary>
    public bool Equals(Artist? other) => NativeEquals(Native.cna_artist_equals, other);

    public override bool Equals(object? obj) => Equals(obj as Artist);

    public override int GetHashCode() => ReadInt(Native.cna_artist_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(Artist? left, Artist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Artist? left, Artist? right) => !(left == right);
}
