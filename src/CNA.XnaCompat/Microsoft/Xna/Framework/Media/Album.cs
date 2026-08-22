namespace Microsoft.Xna.Framework.Media;

public sealed class Album : IDisposable, IEquatable<Album>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.Album> _object;
    private SongCollection? _songs;

    internal Album(CNA.Media.Album inner)
    {
        _object = new(inner);
    }

    ~Album()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.Album Inner => _object.Inner;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public Artist? Artist => Inner.Artist is { } artist ? new Artist(artist) : null;

    public SongCollection Songs => _songs ??= new SongCollection(Inner.Songs);

    public Genre? Genre => Inner.Genre is { } genre ? new Genre(genre) : null;

    public TimeSpan Duration => Inner.Duration;

    public bool HasArt => Inner.HasArt;

    public Stream GetAlbumArt() => Inner.GetAlbumArt();

    public Stream GetThumbnail() => Inner.GetThumbnail();

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Album? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as Album);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(Album? first, Album? second) => object.Equals(first, second);

    public static bool operator !=(Album? first, Album? second) => !(first == second);
}
