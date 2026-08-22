namespace Microsoft.Xna.Framework.Media;

public sealed class Picture : IDisposable, IEquatable<Picture>
{
    private readonly MediaLibraryObjectAdapter<CNA.Media.Picture> _object;

    internal Picture(CNA.Media.Picture inner)
    {
        _object = new(inner);
    }

    ~Picture()
    {
        _object?.ReleaseHandleOnly();
    }

    internal CNA.Media.Picture Inner => _object.Inner;

    internal string Token => Inner.Token;

    public bool IsDisposed => _object.IsDisposed;

    public string Name => Inner.Name;

    public PictureAlbum? Album => Inner.Album is { } album ? new PictureAlbum(album) : null;

    public int Width => Inner.Width;

    public int Height => Inner.Height;

    public DateTime Date => Inner.Date;

    public Stream GetImage() => Inner.GetImage();

    public Stream GetThumbnail() => Inner.GetThumbnail();

    public void Dispose()
    {
        _object.Dispose();
        GC.SuppressFinalize(this);
    }

    public bool Equals(Picture? other) => other is not null && _object.Equals(other._object);

    public override bool Equals(object? obj) => Equals(obj as Picture);

    public override int GetHashCode() => _object.GetHashCodeValue();

    public override string ToString() => Name;

    public static bool operator ==(Picture? first, Picture? second) => object.Equals(first, second);

    public static bool operator !=(Picture? first, Picture? second) => !(first == second);
}
