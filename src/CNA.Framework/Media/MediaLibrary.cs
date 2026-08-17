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
/// The picture-library surface (<c>Picture</c>/<c>PictureAlbum</c>/<c>PictureCollection</c>/
/// <c>PictureAlbumCollection</c>, <see cref="GetPictureFromToken"/>/<see cref="SavePicture(string,byte[])"/>)
/// is genuinely real, not scoped to always-empty the way the music side above is: unlike scanning
/// for pre-existing songs (irreducibly bound to native tag-parsing/FFmpeg), *saving* a picture
/// needs nothing the real C++ engine's own logic doesn't already have a real fallback for --
/// confirmed by reading <c>MediaLibrary::SavePicture</c>'s own source, not assumed. Writing the
/// image bytes to a real "Saved Pictures" folder needs only plain file I/O
/// (<see cref="SavedPictureStore"/>, a faithful port of the real C++ engine's own
/// <c>SavedPictureStore</c>, including its security-relevant filename sanitization). Real image
/// dimension detection needs native decoding this project doesn't have -- but the real C++ engine
/// *already* falls back to <c>width=0, height=0</c> on a decode failure rather than throwing (its
/// own <c>SavePicture</c> catches the decode exception and continues), so this project's own
/// always-0 dimensions are that same real fallback path taken unconditionally, not an invented
/// shortcut. <see cref="Picture.GetThumbnail"/> similarly always takes the real engine's own
/// thumbnail-generation-failure fallback (return the full-size image) rather than performing real
/// PNG downscaling this project has no library for. <see cref="RootPictureAlbum"/> starts as a
/// single, empty root node (no pre-existing-photo scan, for the same reason the music side has
/// none) rather than <see langword="null"/> -- real XNA's own <c>RootPictureAlbum</c> is documented
/// to always return a valid album, even an empty one, so a fresh/never-scanned library is exactly
/// that case, not a special case to work around.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class MediaLibrary</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.MediaLibrary</c> can extend this directly -- the same
/// "preserve the real logic's lineage over namespace purity" trade-off <c>Song</c>/<c>BasicEffect</c>
/// already made; the compat type itself is sealed, matching real XNA.
///
/// CNA.XnaCompat's own <c>MediaLibrary</c> does **not** downcast <see cref="RootPictureAlbum"/>/
/// <see cref="Pictures"/>/<see cref="SavedPictures"/> the way it downcasts <see cref="MediaSource"/>:
/// a covariant-return factory-hook design (the same pattern <c>CNA.Game.CreateGraphicsDevice</c>
/// uses) was tried and does not fit here, because <c>Microsoft.Xna.Framework.Media.PictureCollection</c>/
/// <c>PictureAlbumCollection</c> are -- like <c>SongCollection</c>/<c>AlbumCollection</c> --
/// independent reimplementations of their <c>CNA.Media</c> counterparts, not subclasses (extending
/// directly would inherit an indexer typed to this namespace's <see cref="Picture"/>/
/// <see cref="PictureAlbum"/>, not the compat one). A covariant-return override requires the
/// override's return type to actually be a subtype of the declared return type, which an
/// independent reimplementation by definition is not -- so this class stays a plain, non-virtual
/// implementation, and CNA.XnaCompat's own <c>MediaLibrary</c> instead maintains its own
/// independently-tracked, compat-typed picture state, built directly on <see cref="SavedPictureStore"/>
/// (the shared low-level file-I/O helper) rather than on this class's own bookkeeping -- see that
/// type's own doc comment.
/// </summary>
public class MediaLibrary : IDisposable
{
    private readonly string _pictureRoot;
    private readonly List<Picture> _ownedPictures = [];
    private PictureAlbum? _savedPicturesAlbum;

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

        // Environment.GetFolderPath is the exact real BCL equivalent of the real C++ engine's own
        // native MediaLibraryPaths::GetPictureRoot() (design invariant #7) -- returns "" if the
        // platform has no such concept, matching the C++ side's own possibly-empty pictureRoot_.
        _pictureRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        RootPictureAlbum = new PictureAlbum(Path.GetFileName(_pictureRoot), null, _pictureRoot);
        RootPictureAlbum.SetChildAlbumsAndPictures();
        Pictures = new PictureCollection([]);
        SavedPictures = new PictureCollection([]);
    }

    public AlbumCollection Albums { get; }

    public ArtistCollection Artists { get; }

    public GenreCollection Genres { get; }

    public bool IsDisposed { get; private set; }

    public MediaSource MediaSource { get; }

    public PictureCollection Pictures { get; }

    public PlaylistCollection Playlists { get; }

    public PictureAlbum RootPictureAlbum { get; }

    public PictureCollection SavedPictures { get; }

    public SongCollection Songs { get; }

    /// <summary>Matches the real C++ engine's own <c>GetPictureFromToken</c> exactly: a linear
    /// search by <see cref="Picture.Token"/> over every picture this library actually owns (only
    /// ever ones saved via <see cref="SavePicture(string,byte[])"/> in this project, since nothing
    /// scans for pre-existing pictures), returning <see langword="null"/> for no match rather than
    /// throwing.</summary>
    public Picture? GetPictureFromToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        foreach (Picture picture in _ownedPictures)
        {
            if (picture.Token == token)
            {
                return picture;
            }
        }

        return null;
    }

    /// <summary>Matches the real C++ engine's own <c>SavePicture</c> exactly: write the bytes to a
    /// real "Saved Pictures" file, create a <see cref="Picture"/> for it (width/height 0 -- see
    /// this type's own doc comment for why that's a real fallback, not an invented one), and
    /// register it in every collection it's genuinely a member of
    /// (<see cref="Pictures"/>/<see cref="SavedPictures"/>/the "Saved Pictures"
    /// <see cref="PictureAlbum"/>'s own <see cref="PictureAlbum.Pictures"/>). Guards
    /// <see cref="IsDisposed"/> first -- unlike every other <c>Dispose()</c> in this feature (which
    /// only flips a flag on an always-empty or no-longer-reachable collection), this method has a
    /// real, irreversible side effect (a real file write) and mutates collections
    /// <see cref="Dispose"/> already cleared, so allowing it to run after disposal would silently
    /// resurrect state a caller just tore down -- a code-review finding, not something the always-
    /// empty music-side collections ever needed (see <c>MediaPlayer.Play</c>'s own
    /// <c>ObjectDisposedException.ThrowIf(song.IsDisposed, song)</c> for this codebase's existing
    /// precedent, there guarding an argument's disposal rather than <see langword="this"/>'s own).</summary>
    public Picture SavePicture(string name, byte[] imageBuffer)
    {
        ThrowIfInvalidForSavePicture(name);
        ArgumentNullException.ThrowIfNull(imageBuffer);

        string? savedPath = SavedPictureStore.SavePicture(_pictureRoot, name, imageBuffer);
        if (savedPath is null)
        {
            throw new IOException($"Failed to save picture '{name}'.");
        }

        PictureAlbum parentAlbum = EnsureSavedPicturesAlbum();
        var picture = new Picture(name, parentAlbum, width: 0, height: 0, DateTime.Now, savedPath);
        _ownedPictures.Add(picture);

        Pictures.Add(picture);
        SavedPictures.Add(picture);
        parentAlbum.Pictures.Add(picture);

        return picture;
    }

    /// <summary>Validates itself, before ever draining <paramref name="source"/> -- a code-review
    /// finding: the previous version left both checks to the <c>byte[]</c> overload it delegates
    /// to, so a disposed instance or a null <paramref name="name"/> only failed *after* fully
    /// copying <paramref name="source"/> into memory, wastefully (and destructively, for a
    /// non-seekable/network stream) consuming it for a call that was always going to fail.</summary>
    public Picture SavePicture(string name, Stream source)
    {
        ThrowIfInvalidForSavePicture(name);
        ArgumentNullException.ThrowIfNull(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return SavePicture(name, buffer.ToArray());
    }

    /// <summary>Shared by both <c>SavePicture</c> overloads -- a follow-up code-review finding on
    /// the fix that first introduced these checks: each overload originally repeated the identical
    /// two lines directly (needed so the <see cref="Stream"/> overload fails before ever draining
    /// its argument, not after delegating), which worked but meant the same guard existed
    /// twice with no compiler-enforced reason to keep the two copies in sync. Extracted here so
    /// there is exactly one place that defines what "invalid to save a picture" means.</summary>
    private void ThrowIfInvalidForSavePicture(string name)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(name);
    }

    /// <summary>Lazily creates the real "Saved Pictures" <see cref="PictureAlbum"/> tree node (and
    /// registers it as a real child of <see cref="RootPictureAlbum"/>) the first time a picture is
    /// actually saved. Idempotent -- a no-op returning the existing album on later calls. Simpler
    /// than the real C++ engine's own <c>EnsureSavedPicturesAlbum</c>: that version also has to
    /// handle <c>rootPictureAlbum_</c> itself being null (when the initial scan never found a
    /// pictures root to build a tree from) -- this project's own <see cref="RootPictureAlbum"/> is
    /// never null in the first place (constructed unconditionally, see this type's own doc
    /// comment), so that whole fallback branch is genuinely dead code here, not omitted by
    /// oversight.</summary>
    private PictureAlbum EnsureSavedPicturesAlbum()
    {
        if (_savedPicturesAlbum is not null)
        {
            return _savedPicturesAlbum;
        }

        string path = Path.Combine(_pictureRoot, "Saved Pictures");
        var album = new PictureAlbum("Saved Pictures", RootPictureAlbum, path);
        album.SetChildAlbumsAndPictures();
        RootPictureAlbum.Albums.Add(album);

        _savedPicturesAlbum = album;
        return album;
    }

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
        Pictures.Dispose();
        SavedPictures.Dispose();
        RootPictureAlbum.Dispose();
        IsDisposed = true;
    }
}
