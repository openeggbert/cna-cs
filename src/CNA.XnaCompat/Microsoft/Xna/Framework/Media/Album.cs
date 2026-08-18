namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Album</c>: a compat-typed view over <c>CNA.Media.Album</c>. See
/// <see cref="MediaLibraryObject{TBase}"/> for why this wraps rather than extends.</summary>
public class Album : MediaLibraryObject<CNA.Media.Album>, IEquatable<Album>
{
    internal Album(CNA.Media.Album inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public Artist? Artist => Inner.Artist is { } artist ? new Artist(artist) : null;

    public Genre? Genre => Inner.Genre is { } genre ? new Genre(genre) : null;

    public TimeSpan Duration => Inner.Duration;

    public bool HasArt => Inner.HasArt;

    public SongCollection Songs => new(Inner.Songs);

    public Stream GetAlbumArt() => Inner.GetAlbumArt();

    public Stream GetThumbnail() => Inner.GetThumbnail();

    public bool Equals(Album? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Album);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Album? left, Album? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Album? left, Album? right) => !(left == right);
}
