namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>Song</c>. Extends <c>CNA.Media.Song</c> directly rather than wrapping it
/// -- unlike most native-backed compat types, <c>Song</c> has no native handle to worry about
/// double-wrapping (construction is pure managed logic, see <c>CNA.Media.Song</c>'s own doc
/// comment), so there's no reason not to inherit unchanged. Sealed here, matching real XNA's own
/// <c>sealed class Song</c> exactly -- <c>CNA.Media.Song</c> itself is deliberately left unsealed
/// so this class can extend it.
///
/// <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/> need <c>new</c> overrides (a real
/// gap a code review caught -- every other new compat type added alongside these got one, this
/// file was simply missed). The downcast getters are safe in *practice* today (nothing in this
/// project ever sets these to anything but <see langword="null"/> -- no <c>MediaLibrary</c> scan
/// exists, see that type's own doc comment), but not *provably* safe by construction the way
/// <c>MediaLibrary.MediaSource</c>'s own downcast is: <see cref="CNA.Media.Song.Album"/>/etc. have
/// an <c>internal set</c>, so a hypothetical future caller within this project's own
/// <c>InternalsVisibleTo</c> boundary could set a compat <see cref="Song"/>'s underlying field to
/// a base-typed instance directly. Flagged here so a future real <c>MediaLibrary</c> scan
/// implementation knows to always construct compat-typed <see cref="Album"/>/<see cref="Artist"/>/
/// <see cref="Genre"/> when populating a compat <see cref="Song"/>'s fields.
/// </summary>
public sealed class Song : CNA.Media.Song
{
    public Song(string fileName, string name = "")
        : base(fileName, name)
    {
    }

    public Song(string fileName, string assetName, int durationMS)
        : base(fileName, assetName, durationMS)
    {
    }

    public new Album? Album
    {
        get => (Album?)base.Album;
        internal set => base.Album = value;
    }

    public new Artist? Artist
    {
        get => (Artist?)base.Artist;
        internal set => base.Artist = value;
    }

    public new Genre? Genre
    {
        get => (Genre?)base.Genre;
        internal set => base.Genre = value;
    }

    /// <summary>
    /// Resolves <paramref name="uri"/> via <c>CNA.Media.Song.ResolvePathFromUri</c> (shared with
    /// <c>CNA.Media.Song.FromUri</c> so the two can't drift apart), but constructs this namespace's
    /// own <see cref="Song"/> directly instead of delegating to the base <c>FromUri</c> and
    /// re-wrapping -- delegating to the base overload would construct (and file-existence-check) a
    /// throwaway <c>CNA.Media.Song</c> only to immediately discard it for this one, checking the
    /// same file twice for no benefit.
    /// </summary>
    public static new Song FromUri(string name, string uri)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new Song(CNA.Media.Song.ResolvePathFromUri(uri), name);
    }
}
