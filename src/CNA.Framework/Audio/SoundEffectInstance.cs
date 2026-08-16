using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// A playable instance of a <see cref="SoundEffect"/>, created via <see cref="SoundEffect.CreateInstance"/>
/// -- matching real XNA, where <c>SoundEffectInstance</c> has no public constructor of its own
/// (confirmed against the real C++ engine's implementation, which makes its constructor
/// <c>private</c> with <see cref="SoundEffect"/> as the only friend, explicitly documented there
/// as "direct external construction is not part of the public API"). See
/// <see cref="CNA.Interop.Native"/>'s audio section for the ABI-shape reasoning.
/// </summary>
public class SoundEffectInstance : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private bool _hasBeenPlayed;

    /// <summary><c>protected internal</c>, not <c>public</c> -- matches real XNA (no public
    /// constructor) and the real C++ engine (constructor is <c>private</c>, friend-only to
    /// <c>SoundEffect</c>) as closely as C#'s accessibility model allows, while still letting
    /// <c>CNA.XnaCompat</c>'s <c>SoundEffectInstance</c> subclass forward to it -- same
    /// "protected internal raw-handle constructor" pattern <c>Texture2D</c> already uses.</summary>
    protected internal SoundEffectInstance(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_soundeffectinstance_release(new CnaHandle(h)));
    }

    private nint NativeHandleValue => _handle.DangerousGetHandle();

    public void Play()
    {
        CnaResult result = Native.cna_soundeffectinstance_play(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Play));
        _hasBeenPlayed = true;
    }

    public void Pause()
    {
        CnaResult result = Native.cna_soundeffectinstance_pause(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_soundeffectinstance_resume(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    public void Stop() => Stop(immediate: true);

    /// <summary><paramref name="immediate"/> matching the real C++ engine's own documented
    /// semantics: <c>true</c> cuts playback off immediately; <c>false</c> allows a release tail
    /// to finish (only meaningful for effect types this repository doesn't implement yet, but the
    /// parameter itself is part of real XNA's public API shape).</summary>
    public void Stop(bool immediate)
    {
        CnaResult result = Native.cna_soundeffectinstance_stop(new CnaHandle(NativeHandleValue), immediate ? (byte)1 : (byte)0);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    public SoundState State => (SoundState)Native.cna_soundeffectinstance_get_state(new CnaHandle(NativeHandleValue));

    /// <summary>Range [0, 1], but -- matching the real C++ engine's own documented behavior
    /// ("values are passed through unclamped, matching FNA") -- this setter does not validate or
    /// clamp; an out-of-range value is the caller's responsibility, not an error here.</summary>
    public float Volume
    {
        get => Native.cna_soundeffectinstance_get_volume(new CnaHandle(NativeHandleValue));
        set
        {
            CnaResult result = Native.cna_soundeffectinstance_set_volume(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Volume));
        }
    }

    /// <summary>Range [-1, 1] (one octave down to one octave up), unclamped -- same "passed
    /// through as-is" behavior as <see cref="Volume"/>, per the real C++ engine's documented
    /// semantics.</summary>
    public float Pitch
    {
        get => Native.cna_soundeffectinstance_get_pitch(new CnaHandle(NativeHandleValue));
        set
        {
            CnaResult result = Native.cna_soundeffectinstance_set_pitch(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Pitch));
        }
    }

    /// <summary>Range [-1 (full left), 1 (full right)] -- unlike <see cref="Volume"/>/<see cref="Pitch"/>,
    /// the real C++ engine's setter *does* validate this range and throws
    /// <see cref="ArgumentOutOfRangeException"/>; that validation is reproduced here in managed
    /// code (matching where the real implementation itself performs it) rather than left to a
    /// native <see cref="CnaResult"/> failure.</summary>
    public float Pan
    {
        get => Native.cna_soundeffectinstance_get_pan(new CnaHandle(NativeHandleValue));
        set
        {
            if (value is < -1f or > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Must be between -1 and 1.");
            }

            CnaResult result = Native.cna_soundeffectinstance_set_pan(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Pan));
        }
    }

    /// <summary>Matching the real C++ engine's own documented behavior: throws
    /// <see cref="InvalidOperationException"/> if set after <see cref="Play"/> has already been
    /// called once, reproduced here in managed code for the same reason <see cref="Pan"/>'s range
    /// check is.</summary>
    public bool IsLooped
    {
        get => Native.cna_soundeffectinstance_get_is_looped(new CnaHandle(NativeHandleValue)) != 0;
        set
        {
            if (_hasBeenPlayed)
            {
                throw new InvalidOperationException($"{nameof(IsLooped)} cannot be changed after the instance has already been played.");
            }

            CnaResult result = Native.cna_soundeffectinstance_set_is_looped(new CnaHandle(NativeHandleValue), value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(IsLooped));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
