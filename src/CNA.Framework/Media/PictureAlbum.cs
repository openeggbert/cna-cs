namespace CNA.Media;

/// <summary>
/// A hierarchical album of pictures in a <see cref="MediaLibrary"/>. Real XNA's own constructor is
/// <c>MediaLibrary</c>-only, same reasoning as <c>Album</c>'s own doc comment. Two-phase
/// construction (<see cref="SetChildAlbumsAndPictures"/> runs immediately after the constructor,
/// both <c>internal</c> and both only ever called from <see cref="MediaLibrary"/>) matches the
/// real C++ engine's own <c>PictureAlbum</c>/<c>SetChildAlbumsAndPictures</c> shape exactly --
/// kept even though this project's own picture library never actually needs the recursive
/// tree-building the two-phase split exists to support (see <see cref="MediaLibrary"/>'s own doc
/// comment for why), so a future real scan implementation can drop into the same shape without
/// restructuring this type.
///
/// CNA.XnaCompat's own <c>PictureAlbum</c> does not downcast <see cref="Albums"/>/<see cref="Pictures"/>
/// via a covariant-return factory hook the way <c>CNA.Game.CreateGraphicsDevice</c> downcasts
/// <c>GraphicsDevice</c> -- that pattern was tried and doesn't fit here, since
/// <c>Microsoft.Xna.Framework.Media.PictureAlbumCollection</c>/<c>PictureCollection</c> are
/// independent reimplementations of these types, not subclasses (see <c>CNA.Media.MediaLibrary</c>'s
/// own doc comment for the full reasoning). CNA.XnaCompat's <c>MediaLibrary</c> instead maintains
/// its own independently-tracked, compat-typed picture-album tree.
/// </summary>
public class PictureAlbum : IDisposable, IEquatable<PictureAlbum>
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

    /// <summary>By <see cref="Path"/> (an internal identity key, never publicly exposed on this
    /// type -- unlike <see cref="Picture.Token"/>, real XNA's own <c>PictureAlbum</c> has no
    /// public path/token accessor) -- matches the real C++ engine's own
    /// <c>PictureAlbum::Equals</c> exactly.</summary>
    public bool Equals(PictureAlbum? other) => other is not null && Path == other.Path;

    public override bool Equals(object? obj) => Equals(obj as PictureAlbum);

    public override int GetHashCode() => Path.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(PictureAlbum? left, PictureAlbum? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PictureAlbum? left, PictureAlbum? right) => !(left == right);
}
