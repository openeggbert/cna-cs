namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Thin forwarding static class over <c>CNA.Media.MediaPlayer</c>, same pattern this compat
/// layer's other static subsystems (<c>Mouse</c>, <c>Keyboard</c>) already use.
///
/// <c>Play(SongCollection, ...)</c> is supported (added once a compat <see cref="SongCollection"/>
/// existed for the `MediaLibrary` pass to build one from -- see <c>NEXT.md</c>): calling it doesn't
/// need a compat downcast of anything, just an upcast of the input songs, so the blocker below
/// never applied to it in the first place.
///
/// <c>Queue</c> itself is still deliberately missing -- a real, structural blocker, not an
/// oversight. <c>CNA.Media.MediaPlayer.LoadSong</c> always constructs a base <c>CNA.Media.Song</c>
/// defensive copy internally (matching the real C++ engine's own <c>LoadSong</c> exactly),
/// regardless of the actual runtime type of the <see cref="Song"/> passed to <c>Play</c> -- so
/// <c>CNA.Media.MediaPlayer.Queue</c>'s songs are *never* actually compat-typed, even when this
/// compat layer's own <c>Play(Song)</c>/<c>Play(SongCollection)</c> was called with compat-typed
/// songs. Every other compat type this session built solved an equivalent problem by extending its
/// base type directly (<c>BasicEffect</c>, <c>Song</c> itself) -- but <c>MediaPlayer</c> is a
/// <c>static</c> class, which C# does not allow subclassing at all, so there is no seam here to
/// override <c>LoadSong</c>'s copy-construction the way inheritance solved it everywhere else. A
/// compat <c>Queue</c> property would therefore return songs that silently fail an explicit
/// compat-typed downcast -- unsafe to ship, not just imperfect. Follow-up only if this ever
/// matters enough to justify the compat layer maintaining its own parallel queue mirror instead of
/// forwarding to the base one.
/// </summary>
public static class MediaPlayer
{
    public static event EventHandler<EventArgs>? ActiveSongChanged
    {
        add => CNA.Media.MediaPlayer.ActiveSongChanged += value;
        remove => CNA.Media.MediaPlayer.ActiveSongChanged -= value;
    }

    public static event EventHandler<EventArgs>? MediaStateChanged
    {
        add => CNA.Media.MediaPlayer.MediaStateChanged += value;
        remove => CNA.Media.MediaPlayer.MediaStateChanged -= value;
    }

    public static MediaState State => (MediaState)CNA.Media.MediaPlayer.State;

    public static float Volume
    {
        get => CNA.Media.MediaPlayer.Volume;
        set => CNA.Media.MediaPlayer.Volume = value;
    }

    public static bool IsMuted
    {
        get => CNA.Media.MediaPlayer.IsMuted;
        set => CNA.Media.MediaPlayer.IsMuted = value;
    }

    public static bool IsRepeating
    {
        get => CNA.Media.MediaPlayer.IsRepeating;
        set => CNA.Media.MediaPlayer.IsRepeating = value;
    }

    public static bool IsShuffled
    {
        get => CNA.Media.MediaPlayer.IsShuffled;
        set => CNA.Media.MediaPlayer.IsShuffled = value;
    }

    public static TimeSpan PlayPosition => CNA.Media.MediaPlayer.PlayPosition;

    public static void Play(Song song) => CNA.Media.MediaPlayer.Play(song);

    /// <summary>Converts <paramref name="songs"/> to a base-typed <c>CNA.Media.SongCollection</c>
    /// via <see cref="IReadOnlyList{T}"/>'s own covariance (a compat <see cref="Song"/> already
    /// *is* a <c>CNA.Media.Song</c>, so no per-element conversion is needed, just an upcast) --
    /// this is an input parameter, not <c>Queue</c>'s output, so the class-level doc comment's
    /// downcast concern doesn't apply here.</summary>
    public static void Play(SongCollection songs) => CNA.Media.MediaPlayer.Play(ToBaseSongs(songs));

    public static void Play(SongCollection songs, int index) =>
        CNA.Media.MediaPlayer.Play(ToBaseSongs(songs), index);

    private static CNA.Media.SongCollection ToBaseSongs(SongCollection songs)
    {
        ArgumentNullException.ThrowIfNull(songs);
        return MediaCollectionConversion.ToBase(songs);
    }

    public static void Pause() => CNA.Media.MediaPlayer.Pause();

    public static void Resume() => CNA.Media.MediaPlayer.Resume();

    public static void Stop() => CNA.Media.MediaPlayer.Stop();

    public static void MoveNext() => CNA.Media.MediaPlayer.MoveNext();

    public static void MovePrevious() => CNA.Media.MediaPlayer.MovePrevious();

    public static bool IsVisualizationEnabled
    {
        get => CNA.Media.MediaPlayer.IsVisualizationEnabled;
        set => CNA.Media.MediaPlayer.IsVisualizationEnabled = value;
    }

    /// <summary>Forwards directly, no conversion needed: a compat-typed <paramref name="data"/>
    /// already *is* a <c>CNA.Media.VisualizationData</c> (<see cref="VisualizationData"/> extends it
    /// directly, with no members of its own -- see that type's own doc comment), so it upcasts as
    /// this parameter with no seam for anything to diverge.</summary>
    public static void GetVisualizationData(VisualizationData data) => CNA.Media.MediaPlayer.GetVisualizationData(data);
}
