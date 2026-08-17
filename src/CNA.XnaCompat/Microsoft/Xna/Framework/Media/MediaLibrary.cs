namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>MediaLibrary</c>. Extends <c>CNA.Media.MediaLibrary</c> directly -- safe
/// here in a way <c>MediaPlayer.Queue</c> wasn't: <c>MediaLibrary</c>'s own constructor never
/// constructs any <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/<see cref="Playlist"/>
/// instance at all (every collection starts and stays empty -- see <c>CNA.Media.MediaLibrary</c>'s
/// own doc comment for why), so there is no equivalent of <c>LoadSong</c>'s "always constructs the
/// base type regardless of caller" problem for a compat downcast to ever collide with. The `new`
/// overrides below just replace an always-empty base-typed collection with an always-empty
/// compat-typed one -- there is no real data that could ever diverge between them.
///
/// <see cref="MediaSource"/>'s downcast is safe for a related but distinct reason: this type's own
/// constructors are the *only* way to reach a compat-typed <c>MediaLibrary</c> at all, and both of
/// them always supply a compat-typed <see cref="MediaSource"/> to the base constructor -- so
/// <c>base.MediaSource</c> is guaranteed compat-typed for every actually-reachable compat
/// instance, the same "single construction seam, provably compat-typed" safety condition
/// <c>SpriteFont.Texture</c> already established this session.
///
/// <see cref="RootPictureAlbum"/>/<see cref="Pictures"/>/<see cref="SavedPictures"/>/
/// <see cref="GetPictureFromToken"/>/<see cref="SavePicture(string,byte[])"/> take a genuinely
/// different approach from every other member of this class: they do **not** downcast anything
/// from the base <c>CNA.Media.MediaLibrary</c>. A covariant-return factory-hook design (the same
/// pattern <c>CNA.Game.CreateGraphicsDevice</c> uses for <c>GraphicsDevice</c>) was tried first and
/// does not fit here -- it requires the override's return type to actually be a *subtype* of the
/// base's declared return type, but <see cref="PictureCollection"/>/<see cref="PictureAlbumCollection"/>
/// are, like <see cref="SongCollection"/>/<see cref="AlbumCollection"/>, independent
/// reimplementations of their <c>CNA.Media</c> counterparts rather than subclasses (extending
/// directly would inherit an indexer typed to <c>CNA.Media.Picture</c>/<c>PictureAlbum</c>, not
/// this namespace's own -- see <see cref="SongCollection"/>'s own doc comment). With no safe
/// downcast available for the collections, <see cref="Picture"/>/<see cref="PictureAlbum"/> are
/// independent reimplementations too (seeding a base-typed <see cref="PictureAlbum.Parent"/>/
/// <see cref="Picture.Album"/> from a compat-typed tree would have the same problem one level up).
/// This class therefore maintains its own, fully independent, compat-typed picture-tracking state
/// below, built directly on <c>CNA.Media.SavedPictureStore</c> (the shared low-level, security-
/// sensitive file-I/O helper -- reused rather than reimplemented, since path-traversal
/// sanitization is exactly the kind of logic that must not be duplicated) instead of on the base
/// class's own bookkeeping. This mirrors <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/
/// <see cref="Playlist"/>/their collections' own "independent reimplementation" pattern, just
/// extended to genuinely-growing data instead of always-empty data. One consequence, matching a
/// caveat that already exists for every <c>new</c>-shadowed collection on this class: a caller that
/// explicitly upcasts to <c>CNA.Media.MediaLibrary</c> and calls <c>SavePicture</c> through that
/// reference would write a real file and populate the *base* class's own picture state, invisible
/// to this class's compat-typed <see cref="Pictures"/>/<see cref="SavedPictures"/>/
/// <see cref="RootPictureAlbum"/>. Ported XNA game code never references <c>CNA.Media</c> at all,
/// so this is not a realistic path -- documented here rather than engineered around, the same
/// judgment call this session already made for <c>MediaLibrary.Dispose()</c> not cascading to this
/// class's own collections.
/// </summary>
public sealed class MediaLibrary : CNA.Media.MediaLibrary
{
    private readonly string _pictureRoot;
    private readonly List<Picture> _ownedPictures = [];
    private PictureAlbum? _savedPicturesAlbum;

    public MediaLibrary()
        : this(new MediaSource(MediaSourceType.LocalDevice, "Local Device"))
    {
    }

    public MediaLibrary(MediaSource mediaSource)
        : base(mediaSource)
    {
        _pictureRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        RootPictureAlbum = new PictureAlbum(Path.GetFileName(_pictureRoot), null, _pictureRoot);
        RootPictureAlbum.SetChildAlbumsAndPictures();
    }

    // Deliberately one allocation per instance, not a shared static empty singleton: a prior
    // version of this file shared one instance across every MediaLibrary to save a handful of
    // small allocations, but ReadOnlyMediaCollection<T>.Dispose() mutates a public IsDisposed
    // flag -- a genuine per-instance state, not part of the "always empty" content these
    // collections were actually safe to share. Sharing meant disposing *any* MediaLibrary's
    // Albums silently marked *every* MediaLibrary's Albums disposed, process-wide -- a real
    // regression a code review caught. Reverted: the base class itself already allocates its own
    // 5 collections per instance, so this is not a new cost, just restoring the original shape.
    public new AlbumCollection Albums { get; } = new([]);

    public new ArtistCollection Artists { get; } = new([]);

    public new GenreCollection Genres { get; } = new([]);

    public new MediaSource MediaSource => (MediaSource)base.MediaSource;

    public new PictureCollection Pictures { get; } = new([]);

    public new PlaylistCollection Playlists { get; } = new([]);

    public new PictureAlbum RootPictureAlbum { get; }

    public new PictureCollection SavedPictures { get; } = new([]);

    public new SongCollection Songs { get; } = new([]);

    /// <summary>Independent of the base class's own <c>GetPictureFromToken</c> -- see this type's
    /// own doc comment. Searches only pictures this instance itself has saved.</summary>
    public new Picture? GetPictureFromToken(string token)
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

    /// <summary>Independent of the base class's own <c>SavePicture</c> -- see this type's own doc
    /// comment. Uses <c>CNA.Media.SavedPictureStore</c> directly for the actual file write (the
    /// same security-sensitive sanitization logic the base class's own <c>SavePicture</c> uses),
    /// then builds and registers a compat-typed <see cref="Picture"/>. Guards <c>IsDisposed</c>
    /// first -- same reasoning as <c>CNA.Media.MediaLibrary.SavePicture</c>'s own doc comment.</summary>
    public new Picture SavePicture(string name, byte[] imageBuffer)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(imageBuffer);

        string? savedPath = CNA.Media.SavedPictureStore.SavePicture(_pictureRoot, name, imageBuffer);
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

    /// <summary>Same reasoning as <c>CNA.Media.MediaLibrary.SavePicture(string,Stream)</c>'s own
    /// doc comment: validates before ever draining <paramref name="source"/>.</summary>
    public new Picture SavePicture(string name, Stream source)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return SavePicture(name, buffer.ToArray());
    }

    /// <summary>Same rationale as <c>CNA.Media.MediaLibrary</c>'s own private method of the same
    /// name, kept independent for the same reason the rest of this type's picture tracking is.</summary>
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
}
