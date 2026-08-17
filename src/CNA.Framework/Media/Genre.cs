namespace CNA.Media;

/// <summary>A music genre in a <see cref="MediaLibrary"/>. Same shape and same "no CNAEXT
/// deviation, MediaLibrary-only construction" reasoning as <see cref="Artist"/>'s own doc
/// comment.</summary>
public class Genre : IDisposable, IEquatable<Genre>
{
    internal Genre(string name, AlbumCollection albums, SongCollection songs)
    {
        Name = name;
        Albums = albums;
        Songs = songs;
    }

    public AlbumCollection Albums { get; }

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public SongCollection Songs { get; }

    public void Dispose() => IsDisposed = true;

    /// <summary>By name only -- matches the real C++ engine's own <c>Genre::Equals</c>
    /// exactly.</summary>
    public bool Equals(Genre? other) => other is not null && Name == other.Name;

    public override bool Equals(object? obj) => Equals(obj as Genre);

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Genre? left, Genre? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Genre? left, Genre? right) => !(left == right);
}
