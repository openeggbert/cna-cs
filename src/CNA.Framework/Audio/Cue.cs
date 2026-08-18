using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>Cue</c>: one playable authored sound obtained from
/// <see cref="SoundBank.GetCue"/>.
///
/// Its eight state flags come from one <c>cna_cue_get_info</c> call rather than eight getters,
/// which is how the C API shapes it -- so reading several in a row costs one round trip each, not
/// one per flag. They are read through rather than cached because a cue's state advances on the
/// audio thread between reads.
/// </summary>
public class Cue : IDisposable
{
    private readonly NativeResourceHandle _handle;

    internal Cue(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_cue_destroy(new CnaHandle(h)));
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_cue_get_name_size, Native.cna_cue_copy_name, NativeHandle, nameof(Name));

    public bool IsCreated => GetInfo().IsCreated != 0;

    public bool IsDisposed => GetInfo().IsDisposed != 0;

    public bool IsPaused => GetInfo().IsPaused != 0;

    public bool IsPlaying => GetInfo().IsPlaying != 0;

    public bool IsPrepared => GetInfo().IsPrepared != 0;

    public bool IsPreparing => GetInfo().IsPreparing != 0;

    public bool IsStopped => GetInfo().IsStopped != 0;

    public bool IsStopping => GetInfo().IsStopping != 0;

    public void Play()
    {
        CnaResult result = Native.cna_cue_play(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Play));
    }

    public void Pause()
    {
        CnaResult result = Native.cna_cue_pause(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_cue_resume(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    public void Stop(AudioStopOptions options)
    {
        CnaResult result = Native.cna_cue_stop(NativeHandle, (uint)options);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    public void Apply3D(AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);

        CnaAudioListener nativeListener = listener.ToNative();
        CnaAudioEmitter nativeEmitter = emitter.ToNative();
        CnaResult result = Native.cna_cue_apply_3d(NativeHandle, in nativeListener, in nativeEmitter);
        CnaException.ThrowIfFailed(result, nameof(Apply3D));
    }

    public float GetVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        float value = 0f;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_cue_get_variable(NativeHandle, view, out value));
        CnaException.ThrowIfFailed(result, nameof(GetVariable));
        return value;
    }

    public void SetVariable(string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_cue_set_variable(NativeHandle, view, value));
        CnaException.ThrowIfFailed(result, nameof(SetVariable));
    }

    private CnaCueInfo GetInfo()
    {
        var info = new CnaCueInfo();
        CnaResult result = Native.cna_cue_get_info(NativeHandle, ref info);
        CnaException.ThrowIfFailed(result, "cna_cue_get_info");
        return info;
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
