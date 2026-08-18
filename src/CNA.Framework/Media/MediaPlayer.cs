using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Static media playback controller for a queue of songs.
///
/// <b>Fully native-backed, and this is a correction.</b> A previous pass bound eight of
/// <c>media_player.h</c>'s 41 functions and recorded that <see cref="State"/>/<see cref="Volume"/>/
/// <see cref="IsMuted"/>/<see cref="PlayPosition"/>/<see cref="Queue"/> would "stay deliberately
/// NOT native-backed", mirroring the C++ engine's plain static fields, and that the native queue
/// and <c>move_next</c>/<c>move_previous</c>/<c>is_repeating</c>/<c>is_shuffled</c> were "genuine
/// new capability ... not something the ABI mismatch forces". The reasoning was that reproducing
/// the engine's logic in C# gives the same observable behaviour.
///
/// It does not, and that is the point a header audit made. Reproducing it meant a second queue, a
/// second shuffle order, a second state machine and a second playback timer -- none of which native
/// consulted, while native's own were the ones actually driving the audio device. Two
/// implementations of one behaviour can only agree by coincidence, and every fix to either had to
/// be made twice. All of it is now one round trip each: reads report what the player actually did,
/// including a back-buffer size the device refused or a song the engine advanced past on its own.
///
/// The managed <see cref="System.Diagnostics.Stopwatch"/> that stood in for
/// <see cref="PlayPosition"/> is gone with it. It measured wall-clock time since
/// <c>Play</c> rather than playback position, so it drifted from the real position on any
/// engine-side pause, buffer underrun or seek.
///
/// <see cref="Update"/> is <c>CNAEXT</c> (public here, unlike real XNA, which drives the equivalent
/// through <c>FrameworkDispatcher.Update()</c>). It forwards to <c>cna_media_player_update_ext</c>,
/// the canonical timer and state-transition pump. <see cref="CNA.Game"/>'s base
/// <c>Update(GameTime)</c> calls it for any game that calls <c>base.Update(gameTime)</c>.
/// </summary>
public static class MediaPlayer
{
    private static MediaQueue? _queue;
    private static NativeEventBridge? _activeSongChangedBridge;
    private static NativeEventBridge? _mediaStateChangedBridge;
    private static EventHandler<EventArgs>? _activeSongChanged;
    private static EventHandler<EventArgs>? _mediaStateChanged;
    private static readonly object SubscriptionLock = new();

    /// <summary>
    /// Raised when the active song changes. Native raises it now, rather than this class raising it
    /// from its own <c>Play</c>/<c>MoveNext</c> -- so it also fires when the engine advances the
    /// queue on its own, which the managed version could not observe.
    ///
    /// The canonical event is <b>static</b>, so the subscription belongs to the process rather than
    /// to a game and takes no game handle. It is taken on the first <c>+=</c> and held for the
    /// process, since a static event has no disposal point to release it at.
    /// </summary>
    public static event EventHandler<EventArgs>? ActiveSongChanged
    {
        add
        {
            lock (SubscriptionLock)
            {
                _activeSongChangedBridge ??= NativeEventBridge.Subscribe(
                    () => _activeSongChanged?.Invoke(null, EventArgs.Empty),
                    (callback, context) => Subscribe(
                        Native.cna_media_player_subscribe_active_song_changed_ext, callback, context, nameof(ActiveSongChanged)),
                    registration => Native.cna_media_player_unsubscribe_ext(registration));

                _activeSongChanged += value;
            }
        }
        remove
        {
            lock (SubscriptionLock)
            {
                _activeSongChanged -= value;
            }
        }
    }

    /// <summary>Raised when playback state changes. See <see cref="ActiveSongChanged"/>.</summary>
    public static event EventHandler<EventArgs>? MediaStateChanged
    {
        add
        {
            lock (SubscriptionLock)
            {
                _mediaStateChangedBridge ??= NativeEventBridge.Subscribe(
                    () => _mediaStateChanged?.Invoke(null, EventArgs.Empty),
                    (callback, context) => Subscribe(
                        Native.cna_media_player_subscribe_media_state_changed_ext, callback, context, nameof(MediaStateChanged)),
                    registration => Native.cna_media_player_unsubscribe_ext(registration));

                _mediaStateChanged += value;
            }
        }
        remove
        {
            lock (SubscriptionLock)
            {
                _mediaStateChanged -= value;
            }
        }
    }

    private delegate CnaResult SubscribeFunc(nint callback, nint context, out CnaHandle outRegistration);

    private static CnaHandle Subscribe(SubscribeFunc subscribe, nint callback, nint context, string eventName)
    {
        CnaResult result = subscribe(callback, context, out CnaHandle registration);
        CnaException.ThrowIfFailed(result, eventName);
        return registration;
    }

    public static MediaState State => (MediaState)ReadUInt(Native.cna_media_player_get_state, nameof(State));

    /// <summary>Range [0, 1]. Clamped here before the call so the value a caller reads back matches
    /// what it set for an out-of-range write, rather than depending on whether native clamps or
    /// rejects.</summary>
    public static float Volume
    {
        get
        {
            CnaResult result = Native.cna_media_player_get_volume(CnaAmbientGame.Current, out float value);
            CnaException.ThrowIfFailed(result, nameof(Volume));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_media_player_set_volume(CnaAmbientGame.Current, Math.Clamp(value, 0f, 1f));
            CnaException.ThrowIfFailed(result, nameof(Volume));
        }
    }

    public static bool IsMuted
    {
        get => ReadBool(Native.cna_media_player_get_is_muted, nameof(IsMuted));
        set => WriteBool(Native.cna_media_player_set_is_muted, value, nameof(IsMuted));
    }

    public static bool IsRepeating
    {
        get => ReadBool(Native.cna_media_player_get_is_repeating, nameof(IsRepeating));
        set => WriteBool(Native.cna_media_player_set_is_repeating, value, nameof(IsRepeating));
    }

    public static bool IsShuffled
    {
        get => ReadBool(Native.cna_media_player_get_is_shuffled, nameof(IsShuffled));
        set => WriteBool(Native.cna_media_player_set_is_shuffled, value, nameof(IsShuffled));
    }

    /// <summary>Real XNA's "is the game, rather than the user's own background music, in charge of
    /// what is playing". A game that respects it stays silent while the player's own music
    /// plays.</summary>
    public static bool GameHasControl =>
        ReadBool(Native.cna_media_player_get_game_has_control, nameof(GameHasControl));

    /// <summary>
    /// Toggling this is a real side effect either way: it installs or removes an audio-device
    /// post-mix callback (see <see cref="VisualizationData"/>), which has a genuine cost and is
    /// deliberately absent while disabled.
    ///
    /// The getter reads native rather than a cached flag, which closes a caveat a code-review pass
    /// raised against the cached version: the flag mirrored whether a device-level callback was
    /// installed, and a device failure or format change could drop it without the flag being told.
    /// </summary>
    public static bool IsVisualizationEnabled
    {
        get => ReadBool(Native.cna_media_player_get_is_visualization_enabled, nameof(IsVisualizationEnabled));
        set => WriteBool(Native.cna_media_player_set_is_visualization_enabled, value, nameof(IsVisualizationEnabled));
    }

    /// <summary>How far into the active song playback has reached. A real playback position from
    /// the engine, not wall-clock time since <c>Play</c> -- see this class's own doc
    /// comment.</summary>
    public static TimeSpan PlayPosition
    {
        get
        {
            CnaResult result = Native.cna_media_player_get_play_position_ticks(CnaAmbientGame.Current, out long ticks);
            CnaException.ThrowIfFailed(result, nameof(PlayPosition));
            return TimeSpan.FromTicks(ticks);
        }
    }

    /// <summary>
    /// The process-wide queue the player is playing through.
    ///
    /// Cached rather than rebuilt per read: the handle is borrowed from the player, which owns the
    /// one queue, so every read would otherwise take another handle that only a finalizer would
    /// give back. The queue behind it is the same object either way, and XNA callers compare
    /// <c>MediaPlayer.Queue</c> by reference.
    ///
    /// The cache is dropped when the game it was taken against is disposed
    /// (<see cref="ReleaseGameScopedState"/>). The queue itself is process-wide, but the *handle*
    /// came from <c>cna_media_player_get_queue(game, ...)</c>, and holding one past its game is the
    /// kind of assumption that is cheap to avoid and expensive to be wrong about.
    /// </summary>
    public static MediaQueue Queue
    {
        get
        {
            lock (SubscriptionLock)
            {
                if (_queue is not null)
                {
                    return _queue;
                }

                CnaResult result = Native.cna_media_player_get_queue(CnaAmbientGame.Current, out CnaHandle queue);
                CnaException.ThrowIfFailed(result, nameof(Queue));
                _queue = new MediaQueue(queue);
                return _queue;
            }
        }
    }

    /// <summary>Clears the queue, enqueues <paramref name="song"/> and starts playing. The player
    /// <b>copies</b> the song into its queue rather than taking it, so
    /// <paramref name="song"/> stays the caller's.</summary>
    public static void Play(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        ObjectDisposedException.ThrowIf(song.IsDisposed, song);

        CnaResult result = Native.cna_media_player_play_song(CnaAmbientGame.Current, song.NativeHandle);
        GC.KeepAlive(song);
        CnaException.ThrowIfFailed(result, nameof(Play));
    }

    public static void Play(SongCollection songs)
    {
        ArgumentNullException.ThrowIfNull(songs);

        CnaResult result = Native.cna_media_player_play_songs(CnaAmbientGame.Current, songs.NativeHandle);
        GC.KeepAlive(songs);
        CnaException.ThrowIfFailed(result, nameof(Play));
    }

    public static void Play(SongCollection songs, int index)
    {
        ArgumentNullException.ThrowIfNull(songs);

        CnaResult result = Native.cna_media_player_play_songs_from(CnaAmbientGame.Current, songs.NativeHandle, index);
        GC.KeepAlive(songs);
        CnaException.ThrowIfFailed(result, nameof(Play));
    }

    public static void Pause() => Invoke(Native.cna_media_player_pause, nameof(Pause));

    public static void Resume() => Invoke(Native.cna_media_player_resume, nameof(Resume));

    public static void Stop() => Invoke(Native.cna_media_player_stop, nameof(Stop));

    public static void MoveNext() => Invoke(Native.cna_media_player_move_next, nameof(MoveNext));

    public static void MovePrevious() => Invoke(Native.cna_media_player_move_previous, nameof(MovePrevious));

    /// <summary>Per-frame maintenance -- the canonical timer and state-transition pump. Detecting a
    /// finished song and advancing the queue is native's job now; the managed reimplementation of
    /// that logic is gone with the managed queue it depended on.</summary>
    public static void Update()
    {
        Invoke(Native.cna_media_player_update_ext, nameof(Update));
        ThrowPendingCallbackException();
    }

    /// <summary>Releases the renderer's media resources. Meant for application exit; calling it
    /// earlier simply releases whatever the player had initialized, and playback can start
    /// again.</summary>
    public static void ProgramExit() => Invoke(Native.cna_media_player_program_exit_ext, nameof(ProgramExit));

    /// <summary>
    /// Reports whether <paramref name="activeSong"/> should be considered finished after
    /// <paramref name="elapsed"/>. The canonical fallback detector, for builds with no native
    /// track-stopped signal.
    ///
    /// Delegated to <c>cna_media_player_detect_song_ended_by_elapsed_time_ext</c> rather than
    /// recomputed from <see cref="Song.Duration"/>, which is what this used to do. The comparison
    /// looks trivial enough to duplicate, and its one sharp edge is exactly what a duplicate gets
    /// wrong: a song whose duration is unknown must never report ended, rather than reporting it
    /// immediately at time zero.
    /// </summary>
    public static bool DetectSongEndedByElapsedTime(Song activeSong, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(activeSong);

        CnaResult result = Native.cna_media_player_detect_song_ended_by_elapsed_time_ext(
            activeSong.NativeHandle, elapsed.Ticks, out byte ended);
        GC.KeepAlive(activeSong);
        CnaException.ThrowIfFailed(result, nameof(DetectSongEndedByElapsedTime));
        return ended != 0;
    }

    /// <summary>Fills <paramref name="data"/>'s arrays in place. Always makes the native call
    /// regardless of <see cref="IsVisualizationEnabled"/>: the canonical route is safe either way,
    /// writing all-zero data rather than throwing when disabled or when nothing has been captured
    /// yet.</summary>
    public static void GetVisualizationData(VisualizationData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var native = new CnaVisualizationData();
        CnaResult result = Native.cna_media_player_get_visualization_data(CnaAmbientGame.Current, ref native);
        CnaException.ThrowIfFailed(result, nameof(GetVisualizationData));

        for (int i = 0; i < VisualizationData.Size; i++)
        {
            data.Frequencies[i] = native.Frequencies[i];
            data.Samples[i] = native.Samples[i];
        }
    }

    /// <summary>
    /// Drops the cached <see cref="Queue"/>, so the next read takes a fresh handle against whatever
    /// game is current. Called from <see cref="Game"/>'s disposal, the same hook
    /// <see cref="Audio.Microphone"/> uses for its subscriptions.
    ///
    /// The two event subscriptions are deliberately *not* released here. The header is explicit
    /// that those events are static -- "the subscription belongs to the process rather than to a
    /// game and takes no game handle" -- so tearing them down with a game would silently
    /// unsubscribe handlers the caller never removed.
    /// </summary>
    internal static void ReleaseGameScopedState()
    {
        lock (SubscriptionLock)
        {
            _queue?.Dispose();
            _queue = null;
        }
    }

    /// <summary>Rethrows the first exception an event handler threw. These callbacks return
    /// <c>void</c> to native, so <see cref="NativeEventBridge"/> captures instead; surfacing from
    /// <see cref="Update"/> is the per-frame opportunity this static class has.</summary>
    private static void ThrowPendingCallbackException()
    {
        _activeSongChangedBridge?.ThrowPendingException();
        _mediaStateChangedBridge?.ThrowPendingException();
    }

    private delegate CnaResult VoidFunc(CnaHandle game);

    private delegate CnaResult BoolGetter(CnaHandle game, out byte outValue);

    private delegate CnaResult BoolSetter(CnaHandle game, byte value);

    private delegate CnaResult UIntGetter(CnaHandle game, out uint outValue);

    private static void Invoke(VoidFunc call, string context)
    {
        CnaResult result = call(CnaAmbientGame.Current);
        CnaException.ThrowIfFailed(result, context);
    }

    private static bool ReadBool(BoolGetter getter, string context)
    {
        CnaResult result = getter(CnaAmbientGame.Current, out byte value);
        CnaException.ThrowIfFailed(result, context);
        return value != 0;
    }

    private static void WriteBool(BoolSetter setter, bool value, string context)
    {
        CnaResult result = setter(CnaAmbientGame.Current, value ? (byte)1 : (byte)0);
        CnaException.ThrowIfFailed(result, context);
    }

    private static uint ReadUInt(UIntGetter getter, string context)
    {
        CnaResult result = getter(CnaAmbientGame.Current, out uint value);
        CnaException.ThrowIfFailed(result, context);
        return value;
    }
}
