using System.Diagnostics;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Static media playback controller for a queue of songs. Grounded against the real
/// openeggbert/cna C++ engine's own working (if not yet C-ABI-exposed) <c>MediaPlayer</c>
/// implementation over SDL3_mixer (<c>modules/media/</c>) -- <see cref="Play(Song)"/>/
/// <see cref="Pause"/>/<see cref="Resume"/>/<see cref="Stop"/>/<see cref="MoveNext"/>/
/// <see cref="MovePrevious"/>/<see cref="Update"/>'s logic is reproduced from its actual
/// <c>Play</c>/<c>PlaySong</c>/<c>NextSong</c>/<c>Update</c>, not invented.
///
/// Still deliberately scoped down from that implementation's full surface: no visualization data
/// capture, no <c>Album</c>/<c>Artist</c>/<c>Genre</c>/<c>MediaLibrary</c> scanning subsystem (see
/// <c>Song</c>'s own doc comment) -- both need real infrastructure this project has no equivalent
/// for and real XNA games overwhelmingly don't touch for simple background-music playback.
///
/// <see cref="State"/>/<see cref="Volume"/>/<see cref="IsMuted"/>/<see cref="PlayPosition"/>/
/// <see cref="Queue"/> are plain C# static state, not native queries -- matches the real C++
/// engine's own architecture exactly: <c>state_</c>/<c>volume_</c>/<c>queue_</c> are plain C++
/// static fields set locally by <c>Play</c>/<c>Pause</c>/<c>Resume</c>/<c>Stop</c>/<c>NextSong</c>
/// themselves, and its own playback-position timer uses <c>std::chrono::steady_clock</c> -- a
/// language-level facility, not an ABI call. <see cref="System.Diagnostics.Stopwatch"/> is the
/// exact .NET BCL equivalent (design invariant #7), and unlike the C++ engine's own manual
/// start/stop/accumulate bookkeeping, it already tracks elapsed time correctly across multiple
/// start/stop cycles on its own.
///
/// <see cref="Update"/> is <c>CNAEXT</c> (public here, unlike real XNA, which drives the
/// equivalent logic automatically through <c>FrameworkDispatcher.Update()</c> -- a mechanism this
/// project doesn't implement). Without it, nothing would ever detect a song ending or advance the
/// queue at all, so <see cref="CNA.Game"/>'s own base <c>Update(GameTime)</c> calls it
/// automatically for any game that calls <c>base.Update(gameTime)</c>, the closest equivalent to
/// real XNA's automatic per-frame behavior this project can offer without a real
/// <c>FrameworkDispatcher</c>.
///
/// <see cref="ActiveSongChanged"/>/<see cref="MediaStateChanged"/> are raised synchronously,
/// unlike the real C++ engine's own deferred (flag-then-dispatcher-raises-later) mechanism --
/// simpler, not a simplification of correctness, since this project has no per-frame dispatcher to
/// defer through in the first place.
/// </summary>
public static class MediaPlayer
{
    private static readonly Stopwatch Timer = new();
    private static readonly MediaQueue InnerQueue = new();
    private static readonly Random ShuffleRandom = new();
    private static int _numSongsInQueuePlayed;

    public static event EventHandler<EventArgs>? ActiveSongChanged;

    public static event EventHandler<EventArgs>? MediaStateChanged;

    public static MediaState State { get; private set; } = MediaState.Stopped;

    private static float _volume = 1f;

    public static float Volume
    {
        get => _volume;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            CnaResult result = Native.cna_mediaplayer_set_volume(clamped);
            CnaException.ThrowIfFailed(result, nameof(Volume));
            _volume = clamped;
        }
    }

    private static bool _isMuted;

    public static bool IsMuted
    {
        get => _isMuted;
        set
        {
            CnaResult result = Native.cna_mediaplayer_set_muted(value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(IsMuted));
            _isMuted = value;
        }
    }

    public static bool IsRepeating { get; set; }

    public static bool IsShuffled { get; set; }

    public static TimeSpan PlayPosition => Timer.Elapsed;

    public static MediaQueue Queue => InnerQueue;

    /// <summary>
    /// Starts playing <paramref name="song"/>, matching the real C++ engine's own <c>Play(Song*)</c>:
    /// clears the queue, adds a defensive copy of <paramref name="song"/> to it (see
    /// <see cref="LoadSong"/>), then plays the *original* <paramref name="song"/> object directly
    /// -- not the queue's copy, matching a real asymmetry the C++ engine itself has between this
    /// overload and <see cref="Play(SongCollection,int)"/> (which plays the queue's copy). Kept
    /// faithful rather than "fixed": which object should own the resulting <c>PlayCount</c>
    /// increment is a genuinely ambiguous design choice, not a knowably-wrong one the way
    /// <c>Model.Draw</c>'s bone-index fallback was.
    /// </summary>
    public static void Play(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        ObjectDisposedException.ThrowIf(song.IsDisposed, song);

        InnerQueue.Clear();
        _numSongsInQueuePlayed = 0;
        LoadSong(song);
        InnerQueue.ActiveSongIndex = 0;

        PlaySong(song);

        ActiveSongChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Play(SongCollection songs) => Play(songs, 0);

    /// <summary>Matches the real C++ engine's own <c>Play(SongCollection&amp;, int)</c>: clears the
    /// queue, adds a defensive copy of every song in <paramref name="songs"/>, then plays the
    /// queue's own copy of the song at <paramref name="index"/> (not the original from
    /// <paramref name="songs"/>) -- see <see cref="Play(Song)"/>'s own doc comment for the
    /// asymmetry this has with that overload.</summary>
    public static void Play(SongCollection songs, int index)
    {
        ArgumentNullException.ThrowIfNull(songs);

        InnerQueue.Clear();
        _numSongsInQueuePlayed = 0;
        foreach (Song song in songs)
        {
            LoadSong(song);
        }

        InnerQueue.ActiveSongIndex = index;
        PlaySong(InnerQueue.ActiveSong);

        ActiveSongChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>A defensive copy, not the original reference -- matches the real C++ engine's own
    /// <c>LoadSong</c> exactly (<c>queue_.Add(new Song(song-&gt;getHandle(), song-&gt;getNameProperty()))</c>):
    /// the queue's own <see cref="Song"/> objects track their own <see cref="Song.PlayCount"/>
    /// independently of whatever the caller does with their original <see cref="Song"/>
    /// afterward, and vice versa.</summary>
    private static void LoadSong(Song? song)
    {
        if (song is null)
        {
            return;
        }

        InnerQueue.Add(new Song(song.Handle, song.Name));
    }

    /// <summary>The actual "start native playback" primitive both <see cref="Play(Song)"/> and
    /// <see cref="Play(SongCollection,int)"/> funnel through, matching the real C++ engine's own
    /// <c>PlaySong</c>: reset state to <see cref="MediaState.Stopped"/> unconditionally (the
    /// native call always stops whatever was playing before attempting to load the new song --
    /// see the class-level reasoning this session already worked out for the single-song
    /// overload), throw on failure, then increment <see cref="Song.PlayCount"/> and advance to
    /// <see cref="MediaState.Playing"/> only once the native call actually succeeded.</summary>
    private static void PlaySong(Song? song)
    {
        if (song is null)
        {
            return;
        }

        CnaResult result = Native.cna_mediaplayer_play(song.Handle);

        Timer.Reset();
        SetState(MediaState.Stopped);

        CnaException.ThrowIfFailed(result, nameof(Play));

        song.PlayCount++;
        Timer.Restart();
        SetState(MediaState.Playing);
    }

    public static void Pause()
    {
        if (State != MediaState.Playing)
        {
            return;
        }

        CnaResult result = Native.cna_mediaplayer_pause();
        CnaException.ThrowIfFailed(result, nameof(Pause));

        Timer.Stop();
        SetState(MediaState.Paused);
    }

    public static void Resume()
    {
        if (State != MediaState.Paused)
        {
            return;
        }

        CnaResult result = Native.cna_mediaplayer_resume();
        CnaException.ThrowIfFailed(result, nameof(Resume));

        Timer.Start();
        SetState(MediaState.Playing);
    }

    /// <summary>Also resets every queued song's <see cref="Song.PlayCount"/> to 0, matching the
    /// real C++ engine's own <c>Stop</c> exactly.</summary>
    public static void Stop()
    {
        if (State == MediaState.Stopped)
        {
            return;
        }

        CnaResult result = Native.cna_mediaplayer_stop();
        CnaException.ThrowIfFailed(result, nameof(Stop));

        Timer.Reset();

        foreach (Song song in InnerQueue)
        {
            song.PlayCount = 0;
        }

        SetState(MediaState.Stopped);
    }

    public static void MoveNext() => NextSong(1);

    public static void MovePrevious() => NextSong(-1);

    /// <summary>Matches the real C++ engine's own <c>NextSong</c> exactly: stop first, then pick
    /// the next active index -- wrapping to 0 when repeating past the end, a uniformly random
    /// index when <see cref="IsShuffled"/>, otherwise clamped to the queue's bounds (so calling
    /// <see cref="MoveNext"/> at the last song or <see cref="MovePrevious"/> at the first is a
    /// no-op-at-the-edge, not a wraparound, unless repeating).</summary>
    private static void NextSong(int direction)
    {
        Stop();

        if (InnerQueue.Count == 0)
        {
            return;
        }

        if (IsRepeating && InnerQueue.ActiveSongIndex >= InnerQueue.Count - 1)
        {
            InnerQueue.ActiveSongIndex = 0;
            direction = 0;
        }

        InnerQueue.ActiveSongIndex = IsShuffled
            ? ShuffleRandom.Next(InnerQueue.Count)
            : Math.Clamp(InnerQueue.ActiveSongIndex + direction, 0, InnerQueue.Count - 1);

        Song? nextSong = InnerQueue[InnerQueue.ActiveSongIndex];
        if (nextSong is not null)
        {
            PlaySong(nextSong);
        }

        ActiveSongChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Per-frame maintenance: detects whether the active song has finished and advances the queue
    /// if so. Matches the real C++ engine's own <c>Update</c>, but always uses its
    /// elapsed-time-vs-duration fallback path (<see cref="DetectSongEndedByElapsedTime"/>) -- the
    /// real engine prefers a native track-stopped signal when compiled with native audio support,
    /// but this project's own <c>MediaPlayer</c> native surface was deliberately scoped without an
    /// equivalent signal (see this project's own <c>NEXT.md</c>), so the elapsed-time fallback is
    /// the only detection this project has at all, not a fallback from a preferred path.
    /// </summary>
    public static void Update()
    {
        Song? activeSong = InnerQueue.ActiveSong;
        if (activeSong is null || State != MediaState.Playing)
        {
            return;
        }

        if (!DetectSongEndedByElapsedTime(activeSong, PlayPosition))
        {
            return;
        }

        _numSongsInQueuePlayed++;

        if (_numSongsInQueuePlayed >= InnerQueue.Count)
        {
            _numSongsInQueuePlayed = 0;
            if (!IsRepeating)
            {
                Stop();
                ActiveSongChanged?.Invoke(null, EventArgs.Empty);
                return;
            }
        }

        NextSong(1);
    }

    /// <summary>
    /// Pure comparison, no state read/written -- matches the real C++ engine's own
    /// <c>DetectSongEndedByElapsedTime</c> exactly, including its own documented limitation: a
    /// <see cref="Song"/> with an unset (zero) <see cref="Song.Duration"/> can never be reported as
    /// ended this way, so playback simply never auto-advances for such a song rather than
    /// false-triggering immediately at time zero. Public (matching the real engine's own
    /// <c>CNAEXT</c>-public choice) specifically so it stays testable directly -- <see cref="Update"/>
    /// itself needs a real playing queue to exercise meaningfully, but this pure calculation
    /// doesn't.
    /// </summary>
    public static bool DetectSongEndedByElapsedTime(Song activeSong, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(activeSong);

        return activeSong.Duration > TimeSpan.Zero && elapsed >= activeSong.Duration;
    }

    private static void SetState(MediaState value)
    {
        if (State != value)
        {
            State = value;
            MediaStateChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
