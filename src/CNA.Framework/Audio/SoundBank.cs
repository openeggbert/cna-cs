using CNA.Interop;

namespace CNA.Audio;

/// <summary>Matches real XNA's <c>SoundBank</c>: the compiled cue definitions (<c>.xsb</c>) played
/// against an <see cref="AudioEngine"/>. <see cref="PlayCue(string)"/> is fire-and-forget;
/// <see cref="GetCue"/> hands back a <see cref="Cue"/> the caller controls and must
/// dispose.</summary>
public class SoundBank : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public SoundBank(AudioEngine audioEngine, string filename)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        ArgumentNullException.ThrowIfNull(filename);

        CnaHandle soundBank = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            filename, view => Native.cna_sound_bank_create(audioEngine.NativeHandle, view, out soundBank));
        GC.KeepAlive(audioEngine);
        CnaException.ThrowIfFailed(result, nameof(SoundBank));

        _handle = new NativeResourceHandle(soundBank.AsNint, h => Native.cna_sound_bank_destroy(new CnaHandle(h)));
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>. Every caller
    /// pairs it with <see cref="GC.KeepAlive(object)"/> after the native call: once the handle
    /// value has been read this object can be unreachable, and an unreachable
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> may have its critical finalizer run
    /// <c>destroy</c> while the call is still in flight. Defeating exactly that is what
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> is for, so reading the handle
    /// without keeping its owner alive gives the guarantee up -- see <c>plan.md</c> WP17.
    /// </summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_sound_bank_get_is_disposed(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return value != 0;
        }
    }

    public bool IsInUse
    {
        get
        {
            CnaResult result = Native.cna_sound_bank_get_is_in_use(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsInUse));
            return value != 0;
        }
    }

    public Cue GetCue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaHandle cue = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_sound_bank_get_cue(NativeHandle, view, out cue));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetCue));
        return new Cue(cue.AsNint);
    }

    public void PlayCue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(name, view => Native.cna_sound_bank_play_cue(NativeHandle, view));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(PlayCue));
    }

    public void PlayCue(string name, AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);

        CnaAudioListener nativeListener = listener.ToNative();
        CnaAudioEmitter nativeEmitter = emitter.ToNative();
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_sound_bank_play_cue_3d(NativeHandle, view, in nativeListener, in nativeEmitter));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(PlayCue));
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
