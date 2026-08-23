using CNA.Interop;

namespace CNA.Audio;

/// <summary>Matches real XNA's <c>SoundBank</c>: the compiled cue definitions (<c>.xsb</c>) played
/// against an <see cref="AudioEngine"/>. <see cref="PlayCue(string)"/> is fire-and-forget;
/// <see cref="GetCue"/> hands back a <see cref="Cue"/> the caller controls and must
/// dispose.</summary>
public class SoundBank : IDisposable
{
    // Owned native bank; ParentOwned dependency on the AudioEngine. XNA keeps this strong parent
    // edge and the engine keeps only a weak registration so finalization remains possible.
    private readonly NativeResourceHandle _handle;
    private readonly AudioEngine _audioEngine;

    public SoundBank(AudioEngine audioEngine, string filename)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        ArgumentNullException.ThrowIfNull(filename);
        _audioEngine = audioEngine;

        CnaHandle soundBank = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            filename, view => Native.cna_sound_bank_create(audioEngine.NativeHandle, view, out soundBank));
        GC.KeepAlive(audioEngine);
        CnaException.ThrowIfFailed(result, nameof(SoundBank));

        _handle = new NativeResourceHandle(soundBank.AsNint, h => Native.cna_sound_bank_destroy(new CnaHandle(h)).IsSuccess());
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
        return new Cue(cue.AsNint, _audioEngine);
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

    /// <summary>Raised as this soundbank is disposed, matching real XNA. The subscription is
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
                    CnaResult result = Native.cna_sound_bank_subscribe_disposing_ext(
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
