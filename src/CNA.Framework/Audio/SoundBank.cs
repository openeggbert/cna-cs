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
        CnaException.ThrowIfFailed(result, nameof(SoundBank));

        _handle = new NativeResourceHandle(soundBank.AsNint, h => Native.cna_sound_bank_destroy(new CnaHandle(h)));
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_sound_bank_get_is_disposed(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return value != 0;
        }
    }

    public bool IsInUse
    {
        get
        {
            CnaResult result = Native.cna_sound_bank_get_is_in_use(NativeHandle, out byte value);
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
        CnaException.ThrowIfFailed(result, nameof(GetCue));
        return new Cue(cue.AsNint);
    }

    public void PlayCue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(name, view => Native.cna_sound_bank_play_cue(NativeHandle, view));
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
        CnaException.ThrowIfFailed(result, nameof(PlayCue));
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
