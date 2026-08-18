using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// A loaded, native-backed sound resource, now matching the real, shipped openeggbert/cna C API
/// (<c>audio.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s native-ABI-migration
/// entry, step 9. Creation now needs a game handle (<see cref="CnaAmbientGame.Current"/>, the
/// design this project selected back in step 2 specifically for this) -- no parameterless audio
/// route exists anywhere in the real ABI.
///
/// <see cref="Play()"/> and <see cref="Play(float,float,float)"/> are real. They were previously
/// absent, on the recorded grounds that fire-and-forget playback "relies on XNA's internal instance
/// pool ... which this repository has no equivalent for" -- but <c>audio.h:483</c> and <c>:499</c>
/// are exactly that pool, including the <c>out_played</c> flag that carries the "instance limit
/// reached, returns false" behaviour the note named as the missing part. A header audit found it.
/// The same audit found the four process-wide settings
/// (<see cref="MasterVolume"/>/<see cref="DistanceScale"/>/<see cref="DopplerScale"/>/
/// <see cref="SpeedOfSound"/>) sitting unbound at <c>audio.h:408-471</c>.
/// </summary>
public class SoundEffect : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
        : this(buffer, 0, buffer?.Length ?? 0, sampleRate, channels, loopStart: 0, loopLength: 0)
    {
    }

    /// <summary>
    /// <paramref name="buffer"/> must be headerless, little-endian, signed 16-bit PCM samples --
    /// not a WAV/RIFF file and not an XNB asset, matching real XNA's own constructor and the real
    /// ABI's own documented requirement for <c>cna_sound_effect_create_pcm16_range_ext</c> exactly
    /// (real XNA's own 7-argument constructor and this real ABI's "canonical seven-argument
    /// constructor" turned out to already match). Only validates <paramref name="sampleRate"/> is
    /// positive -- real XNA additionally restricts it to the 8,000-48,000 Hz range with its own
    /// <see cref="ArgumentOutOfRangeException"/>, which isn't reproduced here (lower confidence in
    /// the exact bounds than in the rest of this constructor's validation, so this deliberately
    /// validates less rather than risk enforcing the wrong limits). <paramref name="loopStart"/>/
    /// <paramref name="loopLength"/> are only checked for being non-negative, not for actually
    /// fitting within the sample count implied by <paramref name="count"/> -- the native side is
    /// the one place that can validate a loop region's own units (samples) against the buffer
    /// without duplicating its channel/bit-depth interpretation here.
    /// </summary>
    public unsafe SoundEffect(
        byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        BufferRangeValidation.ValidateRange(buffer.Length, offset, count);
        ArgumentOutOfRangeException.ThrowIfNegative(loopStart);
        ArgumentOutOfRangeException.ThrowIfNegative(loopLength);
        ValidateChannels(channels);

        var createInfo = new CnaSoundEffectCreateInfo
        {
            SampleRate = (uint)sampleRate,
            Channels = (uint)channels,
        };

        fixed (byte* basePtr = buffer)
        {
            CnaResult result = Native.cna_sound_effect_create_pcm16_range_ext(
                CnaAmbientGame.Current, in createInfo, basePtr, (ulong)buffer.Length, offset, count, loopStart, loopLength,
                out CnaHandle handle);
            CnaException.ThrowIfFailed(result, nameof(SoundEffect));
            _handle = new NativeResourceHandle(handle.AsNint, ReleaseNative);
        }
    }

    /// <summary>Wraps an already-created native handle -- used by <c>ContentManager.Load&lt;T&gt;</c>,
    /// same pattern as <c>Texture2D</c>'s equivalent constructor.</summary>
    protected internal SoundEffect(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, ReleaseNative);
    }

    private static void ReleaseNative(nint handleValue) => Native.cna_sound_effect_destroy(new CnaHandle(handleValue));

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    public TimeSpan Duration
    {
        get
        {
            CnaResult result = Native.cna_sound_effect_get_duration_ticks(new CnaHandle(NativeHandleValue), out long ticks);
            CnaException.ThrowIfFailed(result, nameof(Duration));
            return TimeSpan.FromTicks(ticks);
        }
    }

    public SoundEffectInstance CreateInstance() => new(CreateNativeInstanceHandle());

    /// <summary>
    /// Plays once, with no instance to control it. Returns <see langword="false"/> when the engine
    /// already has too many instances playing, matching real XNA -- that is an ordinary answer, not
    /// an error, and so is a disposed effect answering <see langword="false"/>.
    /// </summary>
    public bool Play()
    {
        CnaResult result = Native.cna_sound_effect_play(new CnaHandle(NativeHandleValue), out byte played);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Play));
        return played != 0;
    }

    /// <summary>
    /// Plays once with explicit settings. The canonical asymmetry is preserved and is native's:
    /// <paramref name="pitch"/> is clamped, while <paramref name="pan"/> outside [-1, 1] is
    /// rejected. Reproducing the pan check here as well would give the caller a
    /// <see cref="ArgumentOutOfRangeException"/> rather than a native result, which is what
    /// <see cref="SoundEffectInstance.Pan"/> already does for the same reason.
    /// </summary>
    public bool Play(float volume, float pitch, float pan)
    {
        if (pan is < -1f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(pan), pan, "Must be between -1 and 1.");
        }

        CnaResult result = Native.cna_sound_effect_play_with_settings(
            new CnaHandle(NativeHandleValue), volume, pitch, pan, out byte played);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Play));
        return played != 0;
    }

    /// <summary>The process-wide playback volume, in [0, 1].</summary>
    public static float MasterVolume
    {
        get => GetGlobal(Native.cna_sound_effect_get_master_volume, nameof(MasterVolume));
        set => SetGlobal(Native.cna_sound_effect_set_master_volume, value, nameof(MasterVolume));
    }

    /// <summary>Scales the distance between an <see cref="AudioListener"/> and an
    /// <see cref="AudioEmitter"/> for 3D attenuation.</summary>
    public static float DistanceScale
    {
        get => GetGlobal(Native.cna_sound_effect_get_distance_scale, nameof(DistanceScale));
        set => SetGlobal(Native.cna_sound_effect_set_distance_scale, value, nameof(DistanceScale));
    }

    /// <summary>Scales the Doppler effect applied to 3D playback.</summary>
    public static float DopplerScale
    {
        get => GetGlobal(Native.cna_sound_effect_get_doppler_scale, nameof(DopplerScale));
        set => SetGlobal(Native.cna_sound_effect_set_doppler_scale, value, nameof(DopplerScale));
    }

    /// <summary>The speed of sound used for Doppler, in units per second.</summary>
    public static float SpeedOfSound
    {
        get => GetGlobal(Native.cna_sound_effect_get_speed_of_sound, nameof(SpeedOfSound));
        set => SetGlobal(Native.cna_sound_effect_set_speed_of_sound, value, nameof(SpeedOfSound));
    }

    private delegate CnaResult GetGlobalFunc(CnaHandle game, out float outValue);

    private delegate CnaResult SetGlobalFunc(CnaHandle game, float value);

    /// <summary>These four are static in XNA but game-addressed in the ABI, so they read the
    /// ambient game the same way <c>Keyboard</c>/<c>Mouse</c> do -- see
    /// <c>CnaAmbientGame</c>.</summary>
    private static float GetGlobal(GetGlobalFunc getter, string propertyName)
    {
        CnaResult result = getter(CnaAmbientGame.Current, out float value);
        CnaException.ThrowIfFailed(result, propertyName);
        return value;
    }

    private static void SetGlobal(SetGlobalFunc setter, float value, string propertyName)
    {
        CnaResult result = setter(CnaAmbientGame.Current, value);
        CnaException.ThrowIfFailed(result, propertyName);
    }

    /// <summary>
    /// Creates the native playable-instance handle without wrapping it. <c>internal</c> (visible
    /// to CNA.XnaCompat via the assembly's <c>InternalsVisibleTo</c> grant) so CNA.XnaCompat's
    /// <c>SoundEffect.CreateInstance</c> override can wrap the *same* native call's result in its
    /// own <c>SoundEffectInstance</c> type -- calling <see cref="CreateInstance"/> itself and then
    /// re-wrapping its return value would wrap the same handle twice, double-releasing it on
    /// disposal. Same pattern as <c>RenderTarget2D.CreateNativeHandle</c>.
    /// </summary>
    internal nint CreateNativeInstanceHandle()
    {
        CnaResult result = Native.cna_sound_effect_create_instance(new CnaHandle(NativeHandleValue), out CnaHandle instance);
        CnaException.ThrowIfFailed(result, nameof(CreateInstance));
        return instance.AsNint;
    }

    /// <summary>Pure arithmetic (16-bit-PCM byte size to <see cref="TimeSpan"/>), no native call --
    /// real, testable today, same as the math value types. Rounds down to a whole sample count
    /// (a partial trailing sample has no duration of its own), matching how PCM sample counting
    /// actually works.</summary>
    public static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate, AudioChannels channels)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        ValidateChannels(channels);

        if (sizeInBytes == 0)
        {
            return TimeSpan.Zero;
        }

        int blockAlign = 2 * (int)channels;
        long sampleCount = sizeInBytes / blockAlign;
        return TimeSpan.FromSeconds(sampleCount / (double)sampleRate);
    }

    /// <summary><see cref="AudioChannels"/> is a plain enum, so C# does not itself reject a cast
    /// like <c>(AudioChannels)0</c> -- without this check, that value would make
    /// <see cref="GetSampleDuration"/> divide by zero and <see cref="GetSampleSizeInBytes"/>
    /// silently return 0 for any duration, instead of failing with a clear argument error.
    /// </summary>
    private static void ValidateChannels(AudioChannels channels)
    {
        if (channels is not (AudioChannels.Mono or AudioChannels.Stereo))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), channels, "Must be Mono or Stereo.");
        }
    }

    /// <summary>The inverse of <see cref="GetSampleDuration"/> -- also pure arithmetic. Rounds the
    /// sample count down, then back up to a whole block (sample-aligned across all channels),
    /// matching 16-bit PCM's fixed per-sample byte size.</summary>
    public static int GetSampleSizeInBytes(TimeSpan duration, int sampleRate, AudioChannels channels)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentException("Must not be negative.", nameof(duration));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        ValidateChannels(channels);

        int blockAlign = 2 * (int)channels;
        long sampleCount = (long)(duration.TotalSeconds * sampleRate);
        return (int)(sampleCount * blockAlign);
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
