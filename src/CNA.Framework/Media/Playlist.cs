namespace CNA.Media;

/// <summary>A playlist of songs in a <see cref="MediaLibrary"/>. Same "no CNAEXT deviation,
/// MediaLibrary-only construction" reasoning as <see cref="Artist"/>'s own doc comment.</summary>
public class Playlist : IDisposable, IEquatable<Playlist>
{
    internal Playlist(string name, SongCollection songs, TimeSpan duration)
    {
        Name = name;
        Songs = songs;
        Duration = duration;
    }

    public TimeSpan Duration { get; }

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public SongCollection Songs { get; }

    public void Dispose() => IsDisposed = true;

    /// <summary>By name only -- matches the real C++ engine's own <c>Playlist::Equals</c>
    /// exactly.</summary>
    public bool Equals(Playlist? other) => other is not null && Name == other.Name;

    public override bool Equals(object? obj) => Equals(obj as Playlist);

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Playlist? left, Playlist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Playlist? left, Playlist? right) => !(left == right);
}
