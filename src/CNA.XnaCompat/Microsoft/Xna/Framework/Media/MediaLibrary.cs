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
/// </summary>
public sealed class MediaLibrary : CNA.Media.MediaLibrary
{
    // Shared across every instance rather than allocated per-MediaLibrary (a real code-review
    // finding): every collection here is provably always empty and never mutated, so there is no
    // reason for two different MediaLibrary instances to each own their own separate empty
    // AlbumCollection/etc. -- one shared instance per type is just as correct and avoids churning
    // 5 short-lived wrapper allocations on top of the base constructor's own 5 every time a game
    // constructs a MediaLibrary.
    private static readonly AlbumCollection EmptyAlbums = new([]);
    private static readonly ArtistCollection EmptyArtists = new([]);
    private static readonly GenreCollection EmptyGenres = new([]);
    private static readonly PlaylistCollection EmptyPlaylists = new([]);
    private static readonly SongCollection EmptySongs = new([]);

    public MediaLibrary()
        : this(new MediaSource(MediaSourceType.LocalDevice, "Local Device"))
    {
    }

    public MediaLibrary(MediaSource mediaSource)
        : base(mediaSource)
    {
    }

    public new AlbumCollection Albums => EmptyAlbums;

    public new ArtistCollection Artists => EmptyArtists;

    public new GenreCollection Genres => EmptyGenres;

    public new MediaSource MediaSource => (MediaSource)base.MediaSource;

    public new PlaylistCollection Playlists => EmptyPlaylists;

    public new SongCollection Songs => EmptySongs;
}
