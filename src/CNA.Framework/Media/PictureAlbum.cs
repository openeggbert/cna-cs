using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// One node in a <see cref="MediaLibrary"/>'s picture-album tree: a folder of
/// <see cref="Pictures"/> plus its child <see cref="Albums"/>.
///
/// <see cref="Parent"/> is <see langword="null"/> for the root, which is how a caller walks up.
/// The ABI reports that as an ordinary "not available" answer, matching real XNA's own nullable
/// <c>Parent</c>.
/// </summary>
public class PictureAlbum : MediaLibraryObject, IEquatable<PictureAlbum>
{
    internal PictureAlbum(CnaHandle handle)
        : base(handle, Native.cna_picture_album_dispose, Native.cna_picture_album_get_is_disposed,
               h => Native.cna_picture_album_destroy(h))
    {
    }

    public unsafe string Name => ReadName(
        Native.cna_picture_album_get_name_size, Native.cna_picture_album_copy_name, nameof(Name));

    public PictureAlbum? Parent =>
        ReadOptional(Native.cna_picture_album_get_parent, h => new PictureAlbum(h), nameof(Parent));

    public PictureAlbumCollection Albums => ReadRequired(
        Native.cna_picture_album_get_albums, h => new PictureAlbumCollection(h), nameof(Albums));

    public PictureCollection Pictures => ReadRequired(
        Native.cna_picture_album_get_pictures, h => new PictureCollection(h), nameof(Pictures));

    public bool Equals(PictureAlbum? other) => NativeEquals(Native.cna_picture_album_equals, other);

    public override bool Equals(object? obj) => Equals(obj as PictureAlbum);

    public override int GetHashCode() => ReadInt(Native.cna_picture_album_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(PictureAlbum? left, PictureAlbum? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PictureAlbum? left, PictureAlbum? right) => !(left == right);
}
