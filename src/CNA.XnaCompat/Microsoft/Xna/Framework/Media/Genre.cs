namespace Microsoft.Xna.Framework.Media;

public sealed class Genre : IDisposable, IEquatable<Genre>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.Genre> _object;
    private AlbumCollection? _albums;
    private SongCollection? _songs;

    internal Genre(CNA.Media.Genre inner)
    {
        _object = new(inner);
    }

    ~Genre()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.Genre Inner => _object.Inner;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public SongCollection Songs => _songs ??= new SongCollection(Inner.Songs);

    public AlbumCollection Albums => _albums ??= new AlbumCollection(Inner.Albums);

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Genre? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as Genre);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(Genre? first, Genre? second) => object.Equals(first, second);

    public static bool operator !=(Genre? first, Genre? second) => !(first == second);
}
