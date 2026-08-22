namespace Microsoft.Xna.Framework.Media;

public sealed class Playlist : IDisposable, IEquatable<Playlist>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.Playlist> _object;
    private SongCollection? _songs;

    internal Playlist(CNA.Media.Playlist inner)
    {
        _object = new(inner);
    }

    ~Playlist()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.Playlist Inner => _object.Inner;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public SongCollection Songs => _songs ??= new SongCollection(Inner.Songs);

    public TimeSpan Duration => Inner.Duration;

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Playlist? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as Playlist);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(Playlist? first, Playlist? second) => object.Equals(first, second);

    public static bool operator !=(Playlist? first, Playlist? second) => !(first == second);
}
