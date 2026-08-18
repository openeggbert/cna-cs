using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// One picture in a <see cref="MediaLibrary"/>'s picture tree. Construction is <c>internal</c> --
/// pictures come from a scan or from <see cref="MediaLibrary.SavePicture(string,byte[])"/>.
///
/// <see cref="GetThumbnail"/> returns the same image as <see cref="GetImage"/>: the C API states
/// that CNA generates no separate thumbnail, and calls that canonical behaviour rather than a C
/// limitation. <see cref="Width"/>/<see cref="Height"/> can be zero for an image the loader could
/// not measure -- also canonical, and the reason a caller should not treat zero as an error.
/// </summary>
public class Picture : MediaLibraryObject, IEquatable<Picture>
{
    internal Picture(CnaHandle handle)
        : base(handle, Native.cna_picture_dispose, Native.cna_picture_get_is_disposed,
               h => Native.cna_picture_destroy(h))
    {
    }

    public unsafe string Name => ReadName(Native.cna_picture_get_name_size, Native.cna_picture_copy_name, nameof(Name));

    /// <summary>The key <see cref="MediaLibrary.GetPictureFromToken"/> accepts. CNA uses the
    /// picture's resolved file path, which is also its equality key.</summary>
    public unsafe string Token => ReadName(
        Native.cna_picture_get_token_size_ext, Native.cna_picture_copy_token_ext, nameof(Token));

    public PictureAlbum? Album => ReadOptional(Native.cna_picture_get_album, h => new PictureAlbum(h), nameof(Album));

    /// <summary>When the picture was taken. The ABI reports this as ticks from the Unix epoch --
    /// a point in time, not a duration -- so it is converted here rather than handed over as a raw
    /// <see cref="TimeSpan"/>. A file carrying no timestamp reports whatever the scan recorded,
    /// which may be the epoch itself.</summary>
    public DateTime Date =>
        DateTime.UnixEpoch.AddTicks(ReadTicks(Native.cna_picture_get_date_unix_ticks, nameof(Date)));

    public int Width => ReadInt(Native.cna_picture_get_width, nameof(Width));

    public int Height => ReadInt(Native.cna_picture_get_height, nameof(Height));

    public unsafe Stream GetImage() => OpenBlob(
        Native.cna_picture_get_image_size, Native.cna_picture_copy_image, nameof(GetImage));

    /// <summary>The same bytes as <see cref="GetImage"/> -- see this type's own doc comment for why
    /// that is the canonical answer rather than a shortcut.</summary>
    public unsafe Stream GetThumbnail() => OpenBlob(
        Native.cna_picture_get_thumbnail_size, Native.cna_picture_copy_thumbnail, nameof(GetThumbnail));

    private unsafe Stream OpenBlob(NativeBlobReader.SizeFunc size, NativeBlobReader.CopyFunc copy, string context)
    {
        byte[]? bytes = ReadBlob(size, copy, context);

        // An unreadable or zero-byte image is an empty stream, not an exception: unlike an album
        // with no art (where XNA documents a throw), a picture always has an image by definition --
        // "no bytes" here means the file could not be read back, and a caller copying it into a
        // texture wants that as an empty read rather than as a failure mid-enumeration.
        return new MemoryStream(bytes ?? [], writable: false);
    }

    public bool Equals(Picture? other) => NativeEquals(Native.cna_picture_equals, other);

    public override bool Equals(object? obj) => Equals(obj as Picture);

    public override int GetHashCode() => ReadInt(Native.cna_picture_get_hash_code, nameof(GetHashCode));

    public override string ToString() => Name;

    public static bool operator ==(Picture? left, Picture? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Picture? left, Picture? right) => !(left == right);
}
