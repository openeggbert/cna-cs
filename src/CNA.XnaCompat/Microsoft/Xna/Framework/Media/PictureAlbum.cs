namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureAlbum</c>. Same shape as <see cref="Album"/>;
/// <see cref="Parent"/> is <see langword="null"/> at the root of the tree.</summary>
public class PictureAlbum : MediaLibraryObject<CNA.Media.PictureAlbum>, IEquatable<PictureAlbum>
{
    internal PictureAlbum(CNA.Media.PictureAlbum inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public PictureAlbum? Parent => Inner.Parent is { } parent ? new PictureAlbum(parent) : null;

    public PictureAlbumCollection Albums => new(Inner.Albums);

    public PictureCollection Pictures => new(Inner.Pictures);

    public bool Equals(PictureAlbum? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as PictureAlbum);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(PictureAlbum? left, PictureAlbum? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PictureAlbum? left, PictureAlbum? right) => !(left == right);
}
