namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Picture</c>. Same shape as <see cref="Album"/>.
/// <see cref="GetThumbnail"/> returns the same image as <see cref="GetImage"/> -- canonical
/// behaviour, see <c>CNA.Media.Picture</c>.</summary>
public class Picture : MediaLibraryObject<CNA.Media.Picture>, IEquatable<Picture>
{
    internal Picture(CNA.Media.Picture inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public PictureAlbum? Album => Inner.Album is { } album ? new PictureAlbum(album) : null;

    public DateTime Date => Inner.Date;

    public int Width => Inner.Width;

    public int Height => Inner.Height;

    public Stream GetImage() => Inner.GetImage();

    public Stream GetThumbnail() => Inner.GetThumbnail();

    /// <summary>Not part of real XNA's <c>Picture</c>, which identifies one by object identity.
    /// Exposed because <see cref="MediaLibrary.GetPictureFromToken"/> is real XNA API and needs
    /// something to accept -- see <c>CNA.Media.Picture.Token</c>.</summary>
    public string Token => Inner.Token;

    public bool Equals(Picture? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Picture);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Picture? left, Picture? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Picture? left, Picture? right) => !(left == right);
}
