using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// A music album in a <see cref="MediaLibrary"/>. Real XNA's own constructor is
/// <c>MediaLibrary</c>-only, matching the real C++ engine's <c>private</c>, friended constructor
/// exactly -- kept <c>internal</c> here too, since an album only exists as part of a library scan.
///
/// Every member is a live native round trip over <c>media_library.h</c>. Before the media-library
/// rebinding this was a managed record whose <see cref="HasArt"/> was hardcoded
/// <see langword="false"/>, on the stated grounds that no C ABI existed to scan a real library --
/// which the shipped header contradicts in its first paragraph ("Opening scans the device's music
/// and picture locations").
/// </summary>
public class Album : MediaLibraryObject, IEquatable<Album>
{
    internal Album(CnaHandle handle)
        : base(handle, Native.cna_album_dispose, Native.cna_album_get_is_disposed,
               h => Native.cna_album_destroy(h))
    {
    }

    public unsafe string Name => ReadName(Native.cna_album_get_name_size, Native.cna_album_copy_name, nameof(Name));

    /// <summary><see langword="null"/> for an album whose files name no artist -- the ABI reports
    /// that as an ordinary "not available" answer, not a failure.</summary>
    public Artist? Artist => ReadOptional(Native.cna_album_get_artist, h => new Artist(h), nameof(Artist));

    public Genre? Genre => ReadOptional(Native.cna_album_get_genre, h => new Genre(h), nameof(Genre));

    public TimeSpan Duration => TimeSpan.FromTicks(ReadTicks(Native.cna_album_get_duration, nameof(Duration)));

    public bool HasArt => ReadBool(Native.cna_album_get_has_art, nameof(HasArt));

    public SongCollection Songs => ReadCachedChild(Native.cna_album_get_songs, h => new SongCollection(h), nameof(Songs));

    /// <summary>The album's cover art as an image stream. Throws for an album with no art, matching
    /// real XNA's documented contract -- <see cref="HasArt"/> is how a caller asks first.</summary>
    public unsafe Stream GetAlbumArt() => OpenBlob(
        Native.cna_album_get_art_size, Native.cna_album_copy_art, nameof(GetAlbumArt));

    public unsafe Stream GetThumbnail() => OpenBlob(
        Native.cna_album_get_thumbnail_size, Native.cna_album_copy_thumbnail, nameof(GetThumbnail));

    private unsafe Stream OpenBlob(NativeBlobReader.SizeFunc size, NativeBlobReader.CopyFunc copy, string context)
    {
        byte[]? bytes = ReadBlob(size, copy, context);
        if (bytes is null)
        {
            throw new InvalidOperationException("This album does not have any album art.");
        }

        // Writable: false so a caller cannot mutate the copy and expect it to mean anything.
        return new MemoryStream(bytes, writable: false);
    }

    /// <summary>By (name, artist), not name alone -- album names collide across artists. Delegated
    /// to <c>cna_album_equals</c> rather than reimplemented; see
    /// <see cref="MediaLibraryObject"/>.</summary>
    public bool Equals(Album? other) => NativeEquals(Native.cna_album_equals, other);

    public override bool Equals(object? obj) => Equals(obj as Album);

    public override int GetHashCode() => ReadInt(Native.cna_album_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(Album? left, Album? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Album? left, Album? right) => !(left == right);
}
