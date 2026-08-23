namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Thin forwarding static class over <c>CNA.Media.MediaPlayer</c>, same pattern this compat
/// layer's other static subsystems (<c>Mouse</c>, <c>Keyboard</c>) already use.
///
/// Every member forwards, including <c>Queue</c>, which was deliberately absent before the
/// media-library rebinding. The blocker recorded then was real: compat <see cref="Song"/> extended
/// <c>CNA.Media.Song</c>, while <c>CNA.Media.MediaPlayer.LoadSong</c> always builds a base-typed
/// defensive copy (matching the real C++ engine), so the queue's songs were never compat-typed and
/// a downcasting <c>Queue</c> would have thrown on first use. <c>MediaPlayer</c> is <c>static</c>,
/// so there was no seam to override that copy-construction either. The rebinding moved the whole
/// compat media family from inheritance to composition, which dissolves the problem rather than
/// working around it: a wrapper re-types whatever the base queue holds, so no downcast is involved
/// anywhere.
/// </summary>
public static class MediaPlayer
{
    private static readonly object QueueLock = new();
    private static CNA.Media.MediaQueue? _frameworkQueue;
    private static MediaQueue? _queue;

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

    /// <summary>Real XNA's "is the game, rather than the player's own background music, in charge
    /// of what is playing". Landed when <c>MediaPlayer</c> was rebound onto the full
    /// <c>media_player.h</c> surface -- it was one of the routes that had never been bound.</summary>
    public static bool GameHasControl => CNA.Media.MediaPlayer.GameHasControl;

    public static void Play(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        CNA.Media.MediaPlayer.Play(song.Inner);
    }

    /// <summary>Hands the underlying native collection straight through -- no per-element
    /// conversion and no copy, because a compat <see cref="SongCollection"/> is a view over exactly
    /// the <c>CNA.Media.SongCollection</c> this route wants.</summary>
    public static void Play(SongCollection songs)
    {
        ArgumentNullException.ThrowIfNull(songs);
        CNA.Media.MediaPlayer.Play(songs.Inner);
    }

    public static void Play(SongCollection songs, int index)
    {
        ArgumentNullException.ThrowIfNull(songs);
        CNA.Media.MediaPlayer.Play(songs.Inner, index);
    }

    /// <summary>Real now. It was deliberately absent while compat <see cref="Song"/> was a subclass
    /// of <c>CNA.Media.Song</c>: <c>LoadSong</c> always builds a base-typed defensive copy, so the
    /// queue's songs were never compat-typed and a downcasting <c>Queue</c> would have thrown on
    /// first use. Compat <see cref="Song"/> wraps rather than extends since the media-library
    /// rebinding, and a wrapper does not care what the base queue holds.</summary>
    public static MediaQueue Queue
    {
        get
        {
            lock (QueueLock)
            {
                CNA.Media.MediaQueue frameworkQueue = CNA.Media.MediaPlayer.Queue;
                if (_queue is null || !ReferenceEquals(_frameworkQueue, frameworkQueue))
                {
                    _frameworkQueue = frameworkQueue;
                    _queue = new MediaQueue(frameworkQueue);
                }

                return _queue;
            }
        }
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

    public static void GetVisualizationData(VisualizationData visualizationData)
    {
        ArgumentNullException.ThrowIfNull(visualizationData);
        CNA.Media.MediaPlayer.GetVisualizationData(visualizationData.Framework);
    }

    /// <summary>Releases the renderer's media resources. <c>CNAEXT</c>, like
    /// <c>CNA.Media.MediaPlayer.Update</c> -- real XNA has no equivalent, because its own framework
    /// does this at shutdown.</summary>
    internal static void ProgramExit() => CNA.Media.MediaPlayer.ProgramExit();
}
