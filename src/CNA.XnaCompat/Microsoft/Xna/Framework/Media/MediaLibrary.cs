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
    public MediaLibrary()
        : this(new MediaSource(MediaSourceType.LocalDevice, "Local Device"))
    {
    }

    public MediaLibrary(MediaSource mediaSource)
        : base(mediaSource)
    {
    }

    public new AlbumCollection Albums { get; } = new([]);

    public new ArtistCollection Artists { get; } = new([]);

    public new GenreCollection Genres { get; } = new([]);

    public new MediaSource MediaSource => (MediaSource)base.MediaSource;

    public new PlaylistCollection Playlists { get; } = new([]);

    public new SongCollection Songs { get; } = new([]);
}
