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
    // Owned native cue; ParentOwned dependency on the AudioEngine. XNA intentionally holds the
    // engine rather than the SoundBank that created the cue.
    private readonly NativeResourceHandle _handle;
    private readonly AudioEngine _audioEngine;

    internal Cue(nint nativeHandleValue, AudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_cue_destroy(new CnaHandle(h)).IsSuccess());
        audioEngine.RegisterDependant(this);
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

    public unsafe string Name
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_cue_get_name_size, Native.cna_cue_copy_name, NativeHandle, nameof(Name));
            GC.KeepAlive(this);
            return value;
        }
    }

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
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Play));
    }

    public void Pause()
    {
        CnaResult result = Native.cna_cue_pause(NativeHandle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_cue_resume(NativeHandle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    public void Stop(AudioStopOptions options)
    {
        CnaResult result = Native.cna_cue_stop(NativeHandle, (uint)options);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    public void Apply3D(AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);

        CnaAudioListener nativeListener = listener.ToNative();
        CnaAudioEmitter nativeEmitter = emitter.ToNative();
        CnaResult result = Native.cna_cue_apply_3d(NativeHandle, in nativeListener, in nativeEmitter);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Apply3D));
    }

    public float GetVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        float value = 0f;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_cue_get_variable(NativeHandle, view, out value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetVariable));
        return value;
    }

    public void SetVariable(string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_cue_set_variable(NativeHandle, view, value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetVariable));
    }

    private CnaCueInfo GetInfo()
    {
        var info = new CnaCueInfo();
        CnaResult result = Native.cna_cue_get_info(NativeHandle, ref info);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, "cna_cue_get_info");
        return info;
    }

    public void Dispose()
    {
        if (_handle.IsClosed)
        {
            return;
        }

        NativeEventBridge? disposingBridge = _disposingBridge;
        _disposingBridge = null;
        _disposingHandler = null;
        _handle.Dispose();

        Exception? pending = null;
        if (disposingBridge is not null)
        {
            try
            {
                disposingBridge.ThrowPendingException();
            }
            catch (Exception exception)
            {
                pending = exception;
            }

            try
            {
                disposingBridge.Dispose();
            }
            catch (Exception exception)
            {
                pending ??= exception;
            }
        }

        GC.SuppressFinalize(this);

        if (pending is not null)
        {
            throw pending;
        }
    }

    /// <summary>Raised as this cue is disposed, matching real XNA. The subscription is
    /// taken on the first <c>+=</c> and released with this object -- see
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> for the shared reasoning.</summary>
    public event EventHandler<EventArgs>? Disposing
    {
        add
        {
            _disposingBridge ??= NativeEventBridge.Subscribe(
                () => _disposingHandler?.Invoke(this, EventArgs.Empty),
                (callback, context) =>
                {
                    CnaResult result = Native.cna_cue_subscribe_disposing_ext(
                        NativeHandle, callback, context, out CnaHandle registration);
                    GC.KeepAlive(this);
                    CnaException.ThrowIfFailed(result, nameof(Disposing));
                    return registration;
                },
                registration => Native.cna_audio_unsubscribe_ext(registration));

            _disposingHandler += value;
        }
        remove => _disposingHandler -= value;
    }

    private NativeEventBridge? _disposingBridge;
    private EventHandler<EventArgs>? _disposingHandler;
}
