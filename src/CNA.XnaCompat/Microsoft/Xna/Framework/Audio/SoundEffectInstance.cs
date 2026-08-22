namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0 sound instance facade backed by exactly one CNA-owned instance handle.</summary>
public class SoundEffectInstance : IDisposable
{
    private readonly CNA.Audio.SoundEffectInstance _inner;
    private bool _disposed;

    internal SoundEffectInstance(nint nativeHandleValue)
    {
        _inner = new CNA.Audio.SoundEffectInstance(nativeHandleValue);
    }

    ~SoundEffectInstance()
    {
        Dispose(false);
    }

    internal nint CompatibilityHandle => _inner.NativeHandleValueForCompatibility;

    public bool IsDisposed => _disposed;

    public virtual bool IsLooped
    {
        get => _inner.IsLooped;
        set => _inner.IsLooped = value;
    }

    public float Pan
    {
        get => _inner.Pan;
        set => _inner.Pan = value;
    }

    public float Pitch
    {
        get => _inner.Pitch;
        set => _inner.Pitch = value;
    }

    public SoundState State => (SoundState)(int)_inner.State;

    public float Volume
    {
        get => _inner.Volume;
        set => _inner.Volume = value;
    }

    public virtual void Play() => _inner.Play();

    public void Pause() => _inner.Pause();

    public void Resume() => _inner.Resume();

    public void Stop() => _inner.Stop();

    public void Stop(bool immediate) => _inner.Stop(immediate);

    public void Apply3D(AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);
        _inner.Apply3D(listener, emitter);
    }

    public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listeners);
        ArgumentNullException.ThrowIfNull(emitter);
        _inner.Apply3D(listeners, emitter);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inner.Dispose();
    }
}
