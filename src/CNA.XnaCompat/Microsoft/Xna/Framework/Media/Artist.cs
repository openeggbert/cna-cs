namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Artist</c>. Same shape as <see cref="Album"/>.</summary>
public class Artist : MediaLibraryObject<CNA.Media.Artist>, IEquatable<Artist>
{
    internal Artist(CNA.Media.Artist inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public AlbumCollection Albums => new(Inner.Albums);

    public SongCollection Songs => new(Inner.Songs);

    public bool Equals(Artist? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Artist);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Artist? left, Artist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Artist? left, Artist? right) => !(left == right);
}
