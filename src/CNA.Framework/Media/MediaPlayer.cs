using System.Diagnostics;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Static media playback controller for a single song. Grounded against the real openeggbert/cna
/// C++ engine's own working (if not yet C-ABI-exposed) <c>MediaPlayer</c> implementation over
/// SDL3_mixer (<c>modules/media/</c>) -- <see cref="Play"/>/<see cref="Pause"/>/<see cref="Resume"/>/
/// <see cref="Stop"/>'s state-transition guards are reproduced from its actual logic, not invented.
///
/// Deliberately scoped down from that implementation's full surface, which also includes a
/// <c>MediaQueue</c> (multi-song playlists, shuffle, repeat-driven auto-advance), visualization
/// data capture, and deferred <c>ActiveSongChanged</c>/<c>MediaStateChanged</c> events routed
/// through a <c>FrameworkDispatcher</c> this project doesn't implement. All of that needs either a
/// per-frame <c>Update()</c> call this project has nowhere established to wire into yet, or
/// tracking structures (<c>MediaQueue</c>) with no other user yet -- real, bounded follow-ups,
/// not gaps in this pass. What real XNA games overwhelmingly actually use --
/// <c>MediaPlayer.Play(song)</c> for background music, <c>Volume</c>/<c>IsMuted</c>, and checking
/// <see cref="State"/> -- is what's implemented here.
///
/// <see cref="State"/>/<see cref="Volume"/>/<see cref="IsMuted"/>/<see cref="PlayPosition"/> are
/// plain C# static state, not native queries -- matches the real C++ engine's own architecture
/// exactly: <c>state_</c>/<c>volume_</c> are plain C++ static fields set locally by
/// <c>Play</c>/<c>Pause</c>/<c>Resume</c>/<c>Stop</c> themselves, and its own playback-position
/// timer uses <c>std::chrono::steady_clock</c> -- a language-level facility, not an ABI call.
/// <see cref="System.Diagnostics.Stopwatch"/> is the exact .NET BCL equivalent (design invariant
/// #7), and unlike the C++ engine's own manual start/stop/accumulate bookkeeping, it already
/// tracks elapsed time correctly across multiple start/stop cycles on its own.
/// </summary>
public static class MediaPlayer
{
    private static readonly Stopwatch Timer = new();

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

    /// <summary>Real XNA API a ported game might reference for a repeating queue -- has no effect
    /// here without the per-frame <c>Update()</c>/auto-advance logic this project doesn't
    /// implement yet (see this type's own doc comment), same "tracked but not wired to anything
    /// yet" honesty as <c>GamePadState.PacketNumber</c> always being 0.</summary>
    public static bool IsRepeating { get; set; }

    public static TimeSpan PlayPosition => Timer.Elapsed;

    /// <summary>
    /// Starts playing <paramref name="song"/>, matching the real C++ engine's own
    /// <c>PlaySong</c>: stop whatever's currently playing, reset the position timer, then start
    /// the new song. Unlike the real C++ engine (which silently does nothing on a native load
    /// failure), throws <see cref="CnaException"/> on failure, matching every other native call's
    /// established convention in this project -- but <see cref="State"/> is still reset to
    /// <see cref="MediaState.Stopped"/> first, even on failure: the real <c>PlaySong</c>
    /// unconditionally destroys whatever was previously playing *before* attempting to load the
    /// new song (confirmed in its source), so nothing from a previous song is actually playing
    /// anymore once the native call returns, success or not. The real C++ engine itself leaves
    /// its own <c>state_</c> stale in this exact case (it only ever calls <c>setStateProperty</c>
    /// on the success path) -- a real bug there, not reproduced here, since this project's own
    /// exception-based failure convention makes fixing it straightforward.
    /// </summary>
    public static void Play(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        ObjectDisposedException.ThrowIf(song.IsDisposed, song);

        CnaResult result = Native.cna_mediaplayer_play(song.Handle);

        Timer.Reset();
        State = MediaState.Stopped;

        CnaException.ThrowIfFailed(result, nameof(Play));

        Timer.Restart();
        State = MediaState.Playing;
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
        State = MediaState.Paused;
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
        State = MediaState.Playing;
    }

    public static void Stop()
    {
        if (State == MediaState.Stopped)
        {
            return;
        }

        CnaResult result = Native.cna_mediaplayer_stop();
        CnaException.ThrowIfFailed(result, nameof(Stop));

        Timer.Reset();
        State = MediaState.Stopped;
    }
}
