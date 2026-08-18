namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Genre</c>. Same shape as <see cref="Album"/>.</summary>
public class Genre : MediaLibraryObject<CNA.Media.Genre>, IEquatable<Genre>
{
    internal Genre(CNA.Media.Genre inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public AlbumCollection Albums => new(Inner.Albums);

    public SongCollection Songs => new(Inner.Songs);

    public bool Equals(Genre? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Genre);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Genre? left, Genre? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Genre? left, Genre? right) => !(left == right);
}
