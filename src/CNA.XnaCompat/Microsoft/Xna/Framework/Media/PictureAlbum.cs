namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureAlbum</c>. Independent reimplementation, not a subclass
/// of <c>CNA.Media.PictureAlbum</c> -- see <see cref="MediaLibrary"/>'s own doc comment for why.
/// Internal constructor/<see cref="SetChildAlbumsAndPictures"/>, reachable only from this
/// namespace's own <see cref="MediaLibrary"/>, matching real XNA's own <c>MediaLibrary</c>-only
/// construction and the base type's own two-phase construction shape.</summary>
public sealed class PictureAlbum : IDisposable, IEquatable<PictureAlbum>
{
    internal PictureAlbum(string name, PictureAlbum? parent, string path)
    {
        Name = name;
        Parent = parent;
        Path = path;
    }

    internal void SetChildAlbumsAndPictures()
    {
        Albums = new PictureAlbumCollection([]);
        Pictures = new PictureCollection([]);
    }

    public PictureAlbumCollection Albums { get; private set; } = null!;

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public PictureAlbum? Parent { get; }

    public PictureCollection Pictures { get; private set; } = null!;

    internal string Path { get; }

    public void Dispose() => IsDisposed = true;

    public bool Equals(PictureAlbum? other) => other is not null && Path == other.Path;

    public override bool Equals(object? obj) => Equals(obj as PictureAlbum);

    public override int GetHashCode() => Path.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(PictureAlbum? left, PictureAlbum? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PictureAlbum? left, PictureAlbum? right) => !(left == right);
}
