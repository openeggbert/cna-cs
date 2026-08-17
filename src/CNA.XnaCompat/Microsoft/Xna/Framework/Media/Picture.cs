namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Picture</c>. Independent reimplementation, not a subclass of
/// <c>CNA.Media.Picture</c> -- see <see cref="MediaLibrary"/>'s own doc comment for why: this
/// namespace's <see cref="MediaLibrary"/> maintains its own independently-tracked picture state,
/// so there is no base-constructed instance to safely downcast in the first place. Internal
/// constructor, reachable only from this namespace's own <see cref="MediaLibrary"/>, matching real
/// XNA's own <c>MediaLibrary</c>-only construction.</summary>
public sealed class Picture : IDisposable, IEquatable<Picture>
{
    internal Picture(string name, PictureAlbum? album, int width, int height, DateTime date, string path)
    {
        Name = name;
        Album = album;
        Width = width;
        Height = height;
        Date = date;
        Path = path;
    }

    public PictureAlbum? Album { get; }

    public DateTime Date { get; }

    public int Height { get; }

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public int Width { get; }

    /// <summary>Same rationale as <c>CNA.Media.Picture.Token</c>'s own doc comment.</summary>
    public string Token => Path;

    internal string Path { get; }

    public Stream GetImage() => File.OpenRead(Path);

    public Stream GetThumbnail() => GetImage();

    public void Dispose() => IsDisposed = true;

    public bool Equals(Picture? other) => other is not null && Path == other.Path;

    public override bool Equals(object? obj) => Equals(obj as Picture);

    public override int GetHashCode() => Path.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Picture? left, Picture? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Picture? left, Picture? right) => !(left == right);
}
