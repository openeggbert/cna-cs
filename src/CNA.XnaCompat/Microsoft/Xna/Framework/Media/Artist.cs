namespace Microsoft.Xna.Framework.Media;

public sealed class Artist : IDisposable, IEquatable<Artist>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.Artist> _object;
    private AlbumCollection? _albums;
    private SongCollection? _songs;

    internal Artist(CNA.Media.Artist inner)
    {
        _object = new(inner);
    }

    ~Artist()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.Artist Inner => _object.Inner;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public SongCollection Songs => _songs ??= new SongCollection(Inner.Songs);

    public AlbumCollection Albums => _albums ??= new AlbumCollection(Inner.Albums);

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Artist? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as Artist);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(Artist? first, Artist? second) => object.Equals(first, second);

    public static bool operator !=(Artist? first, Artist? second) => !(first == second);
}
