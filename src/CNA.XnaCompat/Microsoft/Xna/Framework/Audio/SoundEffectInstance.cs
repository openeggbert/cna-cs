namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0 sound instance facade backed by exactly one CNA-owned instance handle.</summary>
public class SoundEffectInstance : IDisposable
{
    private readonly CNA.Audio.SoundEffectInstance _inner;
    // ParentOwned dependency: this wrapper owns the instance handle but strongly roots the
    // SoundEffect whose native data the instance depends on. The parent keeps only a weak child.
    private readonly SoundEffect? _parent;
    private float _volume = 1f;
    private float _pitch;
    private float _pan;
    private bool _isLooped;
    private bool _disposed;

    internal SoundEffectInstance(nint nativeHandleValue)
        : this(nativeHandleValue, parent: null)
    {
    }

    internal SoundEffectInstance(nint nativeHandleValue, SoundEffect? parent)
    {
        _inner = new CNA.Audio.SoundEffectInstance(nativeHandleValue);
        _parent = parent;
    }

    ~SoundEffectInstance()
    {
        Dispose(false);
    }

    internal nint CompatibilityHandle => _inner.NativeHandleValueForCompatibility;

    public bool IsDisposed => _disposed;

    public virtual bool IsLooped
    {
        get => _isLooped;
        set
        {
            ThrowIfDisposed();
            _inner.IsLooped = value;
            _isLooped = value;
        }
    }

    public float Pan
    {
        get => _pan;
        set
        {
            ThrowIfDisposed();
            if (value < -1f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _inner.Pan = value;
            _pan = value;
        }
    }

    public float Pitch
    {
        get => _pitch;
        set
        {
            ThrowIfDisposed();
            if (value < -1f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _inner.Pitch = value;
            _pitch = value;
        }
    }

    public SoundState State
    {
        get
        {
            ThrowIfDisposed();
            return (SoundState)(int)_inner.State;
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            ThrowIfDisposed();
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _inner.Volume = value;
            _volume = value;
        }
    }

    public virtual void Play()
    {
        ThrowIfDisposed();
        _inner.Play();
    }

    public void Pause()
    {
        ThrowIfDisposed();
        _inner.Pause();
    }

    public void Resume()
    {
        ThrowIfDisposed();
        _inner.Resume();
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _inner.Stop();
    }

    public void Stop(bool immediate)
    {
        ThrowIfDisposed();
        _inner.Stop(immediate);
    }

    public void Apply3D(AudioListener listener, AudioEmitter emitter)
    {
        ThrowIfDisposed();
        _inner.Apply3D(listener.ToFramework(), emitter.ToFramework());
    }

    public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
    {
        ThrowIfDisposed();
        var frameworkListeners = new CNA.Audio.AudioListener[listeners.Length];
        for (int i = 0; i < listeners.Length; i++)
        {
            frameworkListeners[i] = listeners[i].ToFramework();
        }

        _inner.Apply3D(frameworkListeners, emitter.ToFramework());
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
        _inner?.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
