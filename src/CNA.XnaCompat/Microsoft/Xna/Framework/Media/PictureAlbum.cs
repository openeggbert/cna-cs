namespace Microsoft.Xna.Framework.Media;

public sealed class PictureAlbum : IDisposable, IEquatable<PictureAlbum>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.PictureAlbum> _object;
    private PictureAlbumCollection? _albums;
    private PictureCollection? _pictures;

    internal PictureAlbum(CNA.Media.PictureAlbum inner)
    {
        _object = new(inner);
    }

    ~PictureAlbum()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.PictureAlbum Inner => _object.Inner;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public PictureAlbumCollection Albums => _albums ??= new PictureAlbumCollection(Inner.Albums);

    public PictureCollection Pictures => _pictures ??= new PictureCollection(Inner.Pictures);

    public PictureAlbum? Parent => Inner.Parent is { } parent ? new PictureAlbum(parent) : null;

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(PictureAlbum? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as PictureAlbum);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(PictureAlbum? first, PictureAlbum? second) => object.Equals(first, second);

    public static bool operator !=(PictureAlbum? first, PictureAlbum? second) => !(first == second);
}
