namespace CNA.Media;

/// <summary>
/// A music album in a <see cref="MediaLibrary"/>. Same "no CNAEXT deviation, MediaLibrary-only
/// construction" reasoning as <see cref="Artist"/>'s own doc comment. <see cref="HasArt"/> is
/// hardcoded <see langword="false"/> and <see cref="GetAlbumArt"/>/<see cref="GetThumbnail"/>
/// always throw, matching real XNA's own documented contract for an album with no art -- correct
/// here specifically because no <see cref="Album"/> in this project is ever backed by a real
/// scanned audio file with real embedded/folder art to report in the first place (see
/// <see cref="MediaLibrary"/>'s own doc comment), not a stub standing in for unwritten logic.
/// </summary>
public class Album : IDisposable, IEquatable<Album>
{
    internal Album(string name, Artist? artist, Genre? genre, TimeSpan duration, SongCollection songs)
    {
        Name = name;
        Artist = artist;
        Genre = genre;
        Duration = duration;
        Songs = songs;
    }

    public Artist? Artist { get; }

    public TimeSpan Duration { get; }

    public Genre? Genre { get; }

    public bool HasArt => false;

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public SongCollection Songs { get; }

    public Stream GetAlbumArt() =>
        throw new InvalidOperationException("This album does not have any album art.");

    public Stream GetThumbnail() =>
        throw new InvalidOperationException("This album does not have any album art.");

    public void Dispose() => IsDisposed = true;

    /// <summary>By (<see cref="Name"/>, <see cref="Artist"/>), not <see cref="Name"/> alone --
    /// matches the real C++ engine's own <c>Album::Equals</c> exactly (album names can collide
    /// across different artists).</summary>
    public bool Equals(Album? other)
    {
        if (other is null || Name != other.Name)
        {
            return false;
        }

        if (ReferenceEquals(Artist, other.Artist))
        {
            return true;
        }

        return Artist is not null && other.Artist is not null && Artist.Equals(other.Artist);
    }

    public override bool Equals(object? obj) => Equals(obj as Album);

    public override int GetHashCode() => HashCode.Combine(Name, Artist);

    public override string ToString() => Name;

    public static bool operator ==(Album? left, Album? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Album? left, Album? right) => !(left == right);
}
