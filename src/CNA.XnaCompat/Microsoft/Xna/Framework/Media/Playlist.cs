namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Playlist</c>. Same shape as <see cref="Album"/>.</summary>
public class Playlist : MediaLibraryObject<CNA.Media.Playlist>, IEquatable<Playlist>
{
    internal Playlist(CNA.Media.Playlist inner)
        : base(inner)
    {
    }

    public string Name => Inner.Name;

    public TimeSpan Duration => Inner.Duration;

    public SongCollection Songs => new(Inner.Songs);

    public bool Equals(Playlist? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Playlist);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(Playlist? left, Playlist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Playlist? left, Playlist? right) => !(left == right);
}
