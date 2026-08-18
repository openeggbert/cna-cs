using CNA.Interop;

namespace CNA.Media;

/// <summary>A playlist of songs in a <see cref="MediaLibrary"/>. Same shape as
/// <see cref="Artist"/>; it has a <see cref="Duration"/> but no albums.</summary>
public class Playlist : MediaLibraryObject, IEquatable<Playlist>
{
    internal Playlist(CnaHandle handle)
        : base(handle, Native.cna_playlist_dispose, Native.cna_playlist_get_is_disposed,
               h => Native.cna_playlist_destroy(h))
    {
    }

    public unsafe string Name => ReadName(Native.cna_playlist_get_name_size, Native.cna_playlist_copy_name, nameof(Name));

    public TimeSpan Duration => TimeSpan.FromTicks(ReadTicks(Native.cna_playlist_get_duration, nameof(Duration)));

    public SongCollection Songs =>
        ReadRequired(Native.cna_playlist_get_songs, h => new SongCollection(h), nameof(Songs));

    public bool Equals(Playlist? other) => NativeEquals(Native.cna_playlist_equals, other);

    public override bool Equals(object? obj) => Equals(obj as Playlist);

    public override int GetHashCode() => ReadInt(Native.cna_playlist_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(Playlist? left, Playlist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Playlist? left, Playlist? right) => !(left == right);
}
