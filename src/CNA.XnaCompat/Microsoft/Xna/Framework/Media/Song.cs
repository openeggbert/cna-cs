namespace Microsoft.Xna.Framework.Media;

public sealed class Song : IDisposable, IEquatable<Song>
{
    internal Song(CNA.Media.Song inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    ~Song()
    {
        Inner?.ReleaseHandleOnly();
    }

    internal CNA.Media.Song Inner { get; }

    public bool IsDisposed => Inner.IsDisposed;

    public string Name => Inner.Name;

    public Artist? Artist => Inner.Artist is { } artist ? new Artist(artist) : null;

    public Album? Album => Inner.Album is { } album ? new Album(album) : null;

    public Genre? Genre => Inner.Genre is { } genre ? new Genre(genre) : null;

    public TimeSpan Duration => Inner.Duration;

    public bool IsRated => Inner.IsRated;

    public int Rating => Inner.Rating;

    public int PlayCount => Inner.PlayCount;

    public int TrackNumber => Inner.TrackNumber;

    public bool IsProtected => Inner.IsProtected;

    public static Song FromUri(string name, Uri uri)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);
        return new Song(CNA.Media.Song.FromUri(name, uri.OriginalString));
    }

    public void Dispose()
    {
        Inner.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Song? other) => other is not null && Inner.Equals(other.Inner);

    public override bool Equals(object? obj) => Equals(obj as Song);

    public override int GetHashCode() => Inner.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Song? first, Song? second) => object.Equals(first, second);

    public static bool operator !=(Song? first, Song? second) => !(first == second);
}
