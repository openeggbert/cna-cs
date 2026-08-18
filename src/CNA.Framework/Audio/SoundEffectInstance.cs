using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// A playable instance of a <see cref="SoundEffect"/>, created via <see cref="SoundEffect.CreateInstance"/>
/// -- matching real XNA, where <c>SoundEffectInstance</c> has no public constructor of its own.
/// Now matches the real, shipped openeggbert/cna C API (<c>audio.h</c>) rather than a self-designed
/// guess -- see <c>NEXT.md</c>'s native-ABI-migration entry, step 9. The real ABI has no individual
/// <c>cna_sound_effect_instance_get_state</c>/<c>_get_volume</c>/<c>_get_pitch</c>/<c>_get_pan</c>/
/// <c>_get_is_looped</c> getters at all -- only one combined snapshot,
/// <c>cna_sound_effect_instance_get_info</c> -- so every getter below now routes through that one
/// native round trip instead of its own. The five individual setters this project originally
/// guessed *do* still exist individually, an asymmetric shape confirmed directly against the real
/// header, not assumed.
/// </summary>
public class SoundEffectInstance : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private bool _hasBeenPlayed;

    /// <summary><c>protected internal</c>, not <c>public</c> -- matches real XNA (no public
    /// constructor) as closely as C#'s accessibility model allows, while still letting
    /// <c>CNA.XnaCompat</c>'s <c>SoundEffectInstance</c> subclass forward to it -- same
    /// "protected internal raw-handle constructor" pattern <c>Texture2D</c> already uses.</summary>
    protected internal SoundEffectInstance(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_sound_effect_instance_destroy(new CnaHandle(h)));
    }

    /// <summary><c>private protected</c> rather than <c>private</c> since Phase 8 WP11b, so
    /// <see cref="DynamicSoundEffectInstance"/> -- a real subclass in real XNA -- can reach its own
    /// handle for the dynamic-only native calls.</summary>
    private protected nint NativeHandleValue => _handle.DangerousGetHandle();

    public void Play()
    {
        CnaResult result = Native.cna_sound_effect_instance_play(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Play));
        _hasBeenPlayed = true;
    }

    public void Pause()
    {
        CnaResult result = Native.cna_sound_effect_instance_pause(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_sound_effect_instance_resume(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    public void Stop() => Stop(immediate: true);

    /// <summary><paramref name="immediate"/> matching the real ABI's own documented semantics:
    /// <c>true</c> cuts playback off immediately; <c>false</c> allows a release tail to finish
    /// (only meaningful for effect types this repository doesn't implement yet, but the parameter
    /// itself is part of real XNA's public API shape).</summary>
    public void Stop(bool immediate)
    {
        CnaResult result = Native.cna_sound_effect_instance_stop(new CnaHandle(NativeHandleValue), immediate ? (byte)1 : (byte)0);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    private CnaSoundEffectInstanceInfo GetInfo()
    {
        var info = new CnaSoundEffectInstanceInfo();
        CnaResult result = Native.cna_sound_effect_instance_get_info(new CnaHandle(NativeHandleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_sound_effect_instance_get_info");
        return info;
    }

    public SoundState State => (SoundState)GetInfo().State;

    /// <summary>Range [0, 1], but -- matching the real ABI's own documented behavior ("passed
    /// through by CNA without clamping") -- this setter does not validate or clamp; an
    /// out-of-range value is the caller's responsibility, not an error here.</summary>
    public float Volume
    {
        get => GetInfo().Volume;
        set
        {
            CnaResult result = Native.cna_sound_effect_instance_set_volume(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Volume));
        }
    }

    /// <summary>Range [-1, 1] (one octave down to one octave up) -- the real ABI clamps this to
    /// [-1, 1] itself (unlike <see cref="Volume"/>'s unclamped pass-through), confirmed directly
    /// against <c>audio.h</c>'s own documented behavior for the getter.</summary>
    public float Pitch
    {
        get => GetInfo().Pitch;
        set
        {
            CnaResult result = Native.cna_sound_effect_instance_set_pitch(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Pitch));
        }
    }

    /// <summary>Range [-1 (full left), 1 (full right)] -- unlike <see cref="Volume"/>/<see cref="Pitch"/>,
    /// the real ABI's setter *does* validate this range and returns a documented failure; that
    /// validation is reproduced here in managed code too (matching where the real C++
    /// implementation itself performs it) rather than left entirely to a native
    /// <see cref="CnaResult"/> failure.</summary>
    public float Pan
    {
        get => GetInfo().Pan;
        set
        {
            if (value is < -1f or > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Must be between -1 and 1.");
            }

            CnaResult result = Native.cna_sound_effect_instance_set_pan(new CnaHandle(NativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(Pan));
        }
    }

    /// <summary>Matching the real ABI's own documented behavior: fails with a documented
    /// <see cref="CnaResult"/> if set after playback has begun. Also checked here in managed code
    /// before reaching native (matching where the real C++ implementation itself performs the same
    /// check), so the failure surfaces as a clear <see cref="InvalidOperationException"/> rather
    /// than a native result the caller has to interpret.</summary>
    public bool IsLooped
    {
        get => GetInfo().IsLooped != 0;
        set
        {
            if (_hasBeenPlayed)
            {
                throw new InvalidOperationException($"{nameof(IsLooped)} cannot be changed after the instance has already been played.");
            }

            CnaResult result = Native.cna_sound_effect_instance_set_is_looped(new CnaHandle(NativeHandleValue), value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(IsLooped));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>The overridable half of disposal. Added in WP15 for
    /// <see cref="DynamicSoundEffectInstance"/>, which holds a native event subscription that must
    /// be released *before* the instance handle it is registered against.</summary>
    protected virtual void Dispose(bool disposing) => _handle.Dispose();

    /// <summary>Matches real XNA's <c>Apply3D</c>: positions this instance in 3D relative to a
    /// listener. Applies to the *instance*, not the effect -- which is why
    /// <c>SoundEffect.Play</c>-style fire-and-forget playback has no 3D
    /// form and <c>SoundEffect.Apply3D</c> goes through
    /// <see cref="SoundEffect.CreateInstance"/>.</summary>
    public void Apply3D(AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);

        CnaAudioListener nativeListener = listener.ToNative();
        CnaAudioEmitter nativeEmitter = emitter.ToNative();
        CnaResult result = Native.cna_sound_effect_instance_apply_3d(
            new CnaHandle(NativeHandleValue), in nativeListener, in nativeEmitter);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Apply3D));
    }
}
