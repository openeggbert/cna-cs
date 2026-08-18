namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>MediaLibrary</c>: a compat-typed view over <c>CNA.Media.MediaLibrary</c>.
///
/// Wraps rather than extends, since the media-library rebinding made the whole family
/// native-backed. The previous version extended the base *and* kept its own independent, managed
/// picture-tracking state built on <c>SavedPictureStore</c>, because no safe downcast existed for
/// the collections -- so a picture saved through a base-typed reference was invisible to this
/// class, a divergence its own doc comment had to document. Wrapping removes the second state
/// entirely: there is one library, one scan, one set of collections.
///
/// Sealed, matching real XNA.
/// </summary>
public sealed class MediaLibrary : IDisposable
{
    private readonly CNA.Media.MediaLibrary _inner;

    public MediaLibrary()
    {
        _inner = new CNA.Media.MediaLibrary();
    }

    public MediaLibrary(MediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        _inner = new CNA.Media.MediaLibrary(mediaSource.Inner);
    }

    public bool IsDisposed => _inner.IsDisposed;

    public MediaSource MediaSource => new(_inner.MediaSource);

    public SongCollection Songs => new(_inner.Songs);

    public AlbumCollection Albums => new(_inner.Albums);

    public ArtistCollection Artists => new(_inner.Artists);

    public GenreCollection Genres => new(_inner.Genres);

    public PlaylistCollection Playlists => new(_inner.Playlists);

    public PictureCollection Pictures => new(_inner.Pictures);

    public PictureCollection SavedPictures => new(_inner.SavedPictures);

    /// <summary><see langword="null"/> on a device with no readable picture location -- see
    /// <c>CNA.Media.MediaLibrary.RootPictureAlbum</c> for why that is reported rather than papered
    /// over with an empty root.</summary>
    public PictureAlbum? RootPictureAlbum =>
        _inner.RootPictureAlbum is { } album ? new PictureAlbum(album) : null;

    public Picture? GetPictureFromToken(string token) =>
        _inner.GetPictureFromToken(token) is { } picture ? new Picture(picture) : null;

    public Picture SavePicture(string name, byte[] imageBuffer) => new(_inner.SavePicture(name, imageBuffer));

    public Picture SavePicture(string name, Stream source) => new(_inner.SavePicture(name, source));

    public void Dispose()
    {
        _inner.Dispose();
        GC.SuppressFinalize(this);
    }
}
