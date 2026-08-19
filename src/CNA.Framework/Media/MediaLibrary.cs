using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// The media library catalog on the current device: the device's music
/// (<see cref="Songs"/>/<see cref="Albums"/>/<see cref="Artists"/>/<see cref="Genres"/>/
/// <see cref="Playlists"/>) and its pictures (<see cref="Pictures"/>/<see cref="SavedPictures"/>/
/// <see cref="RootPictureAlbum"/>).
///
/// <b>Genuinely native-backed.</b> This type and its whole object family used to be an always-empty
/// managed model, on the documented grounds that the real scan depends on ID3/Vorbis tag parsing,
/// FFmpeg duration probing and a native image loader with "no C ABI exposure to build one against".
/// That conclusion was wrong: <c>media_library.h</c> ships the entire surface -- 148 functions --
/// and states in its first paragraph that "opening scans the device's music and picture locations".
/// Everything here now goes through it, including the album art, thumbnails and picture dimensions
/// the old note called irreducibly unreachable.
///
/// Construction really does open (and therefore scan) the library, matching real XNA. An empty
/// result is an ordinary answer rather than a failure -- a device with no music reports counts of
/// zero, and one with no readable picture location has no <see cref="RootPictureAlbum"/> at all,
/// which is why that property is nullable.
///
/// The <c>Begin</c>/<c>End</c> async shape real XNA inherited from the Xbox 360 is not reproduced,
/// because XNA's own <c>MediaLibrary</c> never had one -- unlike <see cref="Storage.StorageDevice"/>,
/// which does and therefore gets both forms.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class MediaLibrary</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.MediaLibrary</c> can extend this directly -- the same "preserve
/// the real logic's lineage over namespace purity" trade-off <c>Song</c>/<c>BasicEffect</c> already
/// made; the compat type itself is sealed, matching real XNA.
/// </summary>
public class MediaLibrary : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public MediaLibrary()
        : this(CreateDefault())
    {
    }

    /// <summary>Opens the library for one enumerated <see cref="MediaSource"/>. Real XNA restricts
    /// this to <see cref="MediaSourceType.LocalDevice"/>, and the check is kept here rather than
    /// left to native: XNA callers expect a <see cref="NotSupportedException"/>, not a native
    /// result code.</summary>
    public MediaLibrary(MediaSource mediaSource)
        : this(CreateFromSource(mediaSource))
    {
    }

    private MediaLibrary(CnaHandle handle)
    {
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_media_library_destroy(new CnaHandle(h)));
    }

    private static CnaHandle CreateDefault()
    {
        CnaResult result = Native.cna_media_library_create(CnaAmbientGame.Current, out CnaHandle library);
        CnaException.ThrowIfFailed(result, nameof(MediaLibrary));
        return library;
    }

    private static CnaHandle CreateFromSource(MediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);

        // Verified against media_library.h rather than assumed: the route "refuses a source whose
        // kind is not the local device with CNA_RESULT_NOT_SUPPORTED, exactly as the canonical
        // constructor refuses it". So this is not a binding limitation and not an ABI one either --
        // XNA's own constructor rejects it, and the ABI mirrors that deliberately. Checked here
        // only so the message names the rule instead of surfacing a bare native failure.
        if (mediaSource.MediaSourceType != MediaSourceType.LocalDevice)
        {
            throw new NotSupportedException(
                $"A media library can only be opened from {nameof(MediaSourceType.LocalDevice)}, not " +
                $"{mediaSource.MediaSourceType}. Real XNA's own MediaLibrary constructor refuses the " +
                "same thing, and the C API mirrors it -- this is the documented behaviour, not a gap.");
        }

        CnaResult result = Native.cna_media_library_create_from_source(
            CnaAmbientGame.Current, mediaSource.Index, out CnaHandle library);
        CnaException.ThrowIfFailed(result, nameof(MediaLibrary));
        return library;
    }

    /// <summary>
    /// Read out of the owning <see cref="NativeResourceHandle"/>. Every caller pairs it with
    /// <see cref="GC.KeepAlive(object)"/>: once the handle value has been read this object can be
    /// unreachable, and an unreachable
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> may have its critical finalizer run
    /// <c>destroy</c> while the call is still in flight -- see <c>plan.md</c> WP17.
    /// </summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed
    {
        get
        {
            if (_handle.IsClosed || _handle.IsInvalid)
            {
                return true;
            }

            CnaResult result = Native.cna_media_library_get_is_disposed(NativeHandle, out byte disposed);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return disposed != 0;
        }
    }

    /// <summary>The source this library was opened for. Rebuilt from what native reports rather
    /// than echoing the constructor argument, so the default-constructed case has a real answer
    /// too.</summary>
    public unsafe MediaSource MediaSource
    {
        get
        {
            CnaResult result = Native.cna_media_library_get_media_source_type(NativeHandle, out uint type);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(MediaSource));

            string name = NativeStringReader.Read(
                Native.cna_media_library_get_media_source_name_size,
                Native.cna_media_library_copy_media_source_name,
                NativeHandle,
                nameof(MediaSource));
            GC.KeepAlive(this);
            return new MediaSource((MediaSourceType)type, name);
        }
    }

    public SongCollection Songs => Read(Native.cna_media_library_get_songs, h => new SongCollection(h), nameof(Songs));

    public AlbumCollection Albums =>
        Read(Native.cna_media_library_get_albums, h => new AlbumCollection(h), nameof(Albums));

    public ArtistCollection Artists =>
        Read(Native.cna_media_library_get_artists, h => new ArtistCollection(h), nameof(Artists));

    public GenreCollection Genres =>
        Read(Native.cna_media_library_get_genres, h => new GenreCollection(h), nameof(Genres));

    public PlaylistCollection Playlists =>
        Read(Native.cna_media_library_get_playlists, h => new PlaylistCollection(h), nameof(Playlists));

    public PictureCollection Pictures =>
        Read(Native.cna_media_library_get_pictures, h => new PictureCollection(h), nameof(Pictures));

    public PictureCollection SavedPictures =>
        Read(Native.cna_media_library_get_saved_pictures, h => new PictureCollection(h), nameof(SavedPictures));

    /// <summary><see langword="null"/> on a device with no readable picture location, which the ABI
    /// reports as an ordinary answer rather than a failure. Real XNA documents this as always
    /// returning a valid album, but real XNA also always has a picture location to return one for;
    /// inventing an empty root for a device that has none would report a directory that does not
    /// exist.</summary>
    public PictureAlbum? RootPictureAlbum
    {
        get
        {
            CnaResult result = Native.cna_media_library_get_root_picture_album(
                NativeHandle, out CnaHandle album, out byte available);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(RootPictureAlbum));
            return available != 0 ? new PictureAlbum(album) : null;
        }
    }

    /// <summary>Finds a picture by its <see cref="Picture.Token"/>. An unknown token answers
    /// <see langword="null"/> rather than throwing, matching both the canonical lookup and real
    /// XNA.</summary>
    public Picture? GetPictureFromToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        CnaHandle picture = default;
        byte available = 0;
        CnaResult result = CnaStringMarshal.WithStringView(
            token, view => Native.cna_media_library_get_picture_from_token(
                NativeHandle, view, out picture, out available));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetPictureFromToken));
        return available != 0 ? new Picture(picture) : null;
    }

    /// <summary>Writes <paramref name="imageBuffer"/> into the device's picture location and adds
    /// the result to <see cref="SavedPictures"/>. An image the loader cannot measure is still
    /// saved, with width and height zero -- canonical behaviour, not a fallback invented
    /// here.</summary>
    public unsafe Picture SavePicture(string name, byte[] imageBuffer)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(imageBuffer);

        CnaHandle picture = default;

        // `fixed` has to be *inside* the lambda: C# forbids a fixed local from being captured by
        // one, and the string view has to outlive the call the same way the pinned buffer does.
        // An empty array pins to null, which the ABI accepts precisely when the count is zero.
        CnaResult result = CnaStringMarshal.WithStringView(name, view =>
        {
            fixed (byte* imagePtr = imageBuffer)
            {
                return Native.cna_media_library_save_picture(
                    NativeHandle, view, imagePtr, (ulong)imageBuffer.Length, out picture);
            }
        });

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SavePicture));
        return new Picture(picture);
    }

    /// <summary>
    /// Validates before draining <paramref name="source"/>: a disposed library or a null
    /// <paramref name="name"/> must not cost the caller a fully-consumed stream for a call that was
    /// always going to fail (destructively so, for a non-seekable or network stream). A code-review
    /// finding on the earlier managed implementation, kept.
    ///
    /// The ABI's own <c>cna_media_library_save_picture_from_stream</c> is deliberately not used:
    /// it takes a <c>CNA_Handle</c> storage stream, "the only byte source this ABI owns", and a
    /// <see cref="Stream"/> here is an arbitrary BCL stream that has no such handle. Reading it into
    /// a buffer and taking the byte-array route is the honest translation.
    /// </summary>
    public Picture SavePicture(string name, Stream source)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return SavePicture(name, buffer.ToArray());
    }

    /// <summary>Canonical disposal (a flag native keeps) followed by the handle release. Neither
    /// result is checked, for the reason <c>Game.Dispose</c> documents: disposal must not
    /// throw.</summary>
    public void Dispose()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return;
        }

        Native.cna_media_library_dispose(NativeHandle);
        GC.KeepAlive(this);
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads one of the library's collections.
    ///
    /// Deliberately builds a fresh wrapper on every property read rather than caching one, and the
    /// caller owns disposing it. Each read takes a new native collection handle, and a cached
    /// wrapper would have to outlive reads that no longer want it while still holding element
    /// handles from earlier ones. Callers that enumerate in a loop should hold the collection in a
    /// local, which is what XNA code does anyway.
    /// </summary>
    private TCollection Read<TCollection>(CollectionFunc getter, Func<CnaHandle, TCollection> wrap, string context)
    {
        CnaResult result = getter(NativeHandle, out CnaHandle collection);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return wrap(collection);
    }

    private delegate CnaResult CollectionFunc(CnaHandle library, out CnaHandle outCollection);
}
