namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>Song</c>: a compat-typed view over <c>CNA.Media.Song</c>.
///
/// Wraps rather than extends, since the media-library rebinding. It used to extend, which worked
/// only while <c>Album</c>/<c>Artist</c>/<c>Genre</c> were always <see langword="null"/> and could
/// be downcast vacuously. They are real native reads now, and a song reached through
/// <see cref="MediaLibrary.Songs"/> is a <c>CNA.Media.Song</c> that no subclass can retroactively
/// become -- so the whole media family moved to composition together, and <see cref="MediaPlayer"/>
/// unwraps at its own call sites instead of relying on an upcast.
///
/// Sealed, matching real XNA's own <c>sealed class Song</c>.
/// </summary>
public sealed class Song : IDisposable, IEquatable<Song>
{
    public Song(string fileName, string name = "")
        : this(new CNA.Media.Song(fileName, name))
    {
    }

    public Song(string fileName, string assetName, int durationMS)
        : this(new CNA.Media.Song(fileName, assetName, durationMS))
    {
    }

    internal Song(CNA.Media.Song inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    internal CNA.Media.Song Inner { get; }

    public string Name => Inner.Name;

    public Album? Album => Inner.Album is { } album ? new Album(album) : null;

    public Artist? Artist => Inner.Artist is { } artist ? new Artist(artist) : null;

    public Genre? Genre => Inner.Genre is { } genre ? new Genre(genre) : null;

    public TimeSpan Duration
    {
        get => Inner.Duration;
        set => Inner.Duration = value;
    }

    public bool IsProtected => Inner.IsProtected;

    public bool IsRated => Inner.IsRated;

    public int PlayCount
    {
        get => Inner.PlayCount;
        set => Inner.PlayCount = value;
    }

    public int Rating => Inner.Rating;

    public int TrackNumber => Inner.TrackNumber;

    public bool IsDisposed => Inner.IsDisposed;

    public void Dispose()
    {
        Inner.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Song? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Song);

    public override int GetHashCode() => Inner.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Song? left, Song? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Song? left, Song? right) => !(left == right);

    /// <summary>Resolves a file URI (or plain path) via <c>CNA.Media.Song.ResolvePathFromUri</c>,
    /// shared with <c>CNA.Media.Song.FromUri</c> so the two cannot drift apart.</summary>
    public static Song FromUri(string name, string uri)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new Song(CNA.Media.Song.ResolvePathFromUri(uri), name);
    }
}
