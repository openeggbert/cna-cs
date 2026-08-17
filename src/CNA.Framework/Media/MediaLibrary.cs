namespace CNA.Media;

/// <summary>
/// Provides access to the media library catalog on the current device.
///
/// **Object model only -- always empty. This is a deliberate, documented scope decision, not an
/// oversight or a "compiles but blocked on the native ABI" placeholder like most of this
/// project's other native-backed types.** The real openeggbert/cna C++ engine's own
/// <c>MediaLibrary::BuildFromRoots</c> depends on infrastructure that has no equivalent anywhere
/// in this binding and no C ABI exposure to build one against: real ID3v2/Vorbis/FLAC tag parsing
/// (<c>CNA::Internal::Media::AudioTagParser</c>), FFmpeg-based audio duration probing
/// (<c>AudioDurationProbe::ProbeDurationMS</c>, built on <c>avformat_find_stream_info</c>), a
/// native directory-scanning index, and a native image loader for cover art. None of this is a
/// "shaped to match a real implementation, just needs porting" situation the way
/// <c>BasicEffect</c>/<c>Model</c>/<c>Song</c> were this session -- the real implementation's
/// actual logic is bound to native media-decoding libraries with no reachable equivalent on
/// either side of this binding today. Reproducing it would mean either a large new native ABI
/// surface upstream (itself needing FFmpeg-equivalent decoding exposed through a C API, a
/// substantial design problem in its own right) or reimplementing binary audio-tag/container
/// parsing from scratch in pure C# -- neither is a reasonable scope for this pass.
///
/// What's implemented instead: the real XNA public API surface -- every type
/// (<see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/<see cref="Playlist"/>/
/// <see cref="MediaSource"/> and their collections), every property, and this constructor's real
/// validation (<see cref="ArgumentNullException"/> for a null <see cref="MediaSource"/>,
/// <see cref="NotSupportedException"/> for a non-<see cref="MediaSourceType.LocalDevice"/> one,
/// both matching the real C++ engine's own checks) -- so ported game code that references these
/// types compiles and runs, but every collection this type exposes is always empty, since nothing
/// ever scans anything. None of <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/
/// <see cref="Playlist"/>'s constructors are <c>CNAEXT</c>-public the way <see cref="Song"/>'s own
/// is, matching real XNA's own choice to keep them <c>MediaLibrary</c>-only (see each type's own
/// doc comment) -- unlike <c>Song</c>, they only make sense as part of a coherent scan this
/// project can't perform, so there's no real reason to hand-build one here either.
///
/// Deliberately out of scope even within this pass, not just deferred by the above: the real C++
/// engine's picture-library surface (<c>Picture</c>/<c>PictureAlbum</c>/<c>PictureCollection</c>/
/// <c>PictureAlbumCollection</c>, <c>GetPictureFromToken</c>/<c>SavePicture</c>) -- a separate,
/// similarly infrastructure-bound feature (native image loading/thumbnailing) that real XNA games
/// essentially never touch (Zune-era personal-photo browsing, not game asset loading), so it
/// wasn't worth pulling into this already-large pass. A ported game referencing
/// <c>MediaLibrary.Pictures</c>/<c>.SavedPictures</c>/<c>.RootPictureAlbum</c> won't compile
/// against this type yet -- a real, narrow, documented gap.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class MediaLibrary</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.MediaLibrary</c> can extend this directly -- the same
/// "preserve the real logic's lineage over namespace purity" trade-off <c>Song</c>/<c>BasicEffect</c>
/// already made; the compat type itself is sealed, matching real XNA.
/// </summary>
public class MediaLibrary : IDisposable
{
    public MediaLibrary()
        : this(new MediaSource(MediaSourceType.LocalDevice, "Local Device"))
    {
    }

    public MediaLibrary(MediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);

        if (mediaSource.MediaSourceType != MediaSourceType.LocalDevice)
        {
            throw new NotSupportedException("Only MediaSourceType.LocalDevice is supported.");
        }

        MediaSource = mediaSource;
        Songs = new SongCollection([]);
        Albums = new AlbumCollection([]);
        Artists = new ArtistCollection([]);
        Genres = new GenreCollection([]);
        Playlists = new PlaylistCollection([]);
    }

    public AlbumCollection Albums { get; }

    public ArtistCollection Artists { get; }

    public GenreCollection Genres { get; }

    public bool IsDisposed { get; private set; }

    public MediaSource MediaSource { get; }

    public PlaylistCollection Playlists { get; }

    public SongCollection Songs { get; }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        Songs.Dispose();
        Albums.Dispose();
        Artists.Dispose();
        Genres.Dispose();
        Playlists.Dispose();
        IsDisposed = true;
    }
}
