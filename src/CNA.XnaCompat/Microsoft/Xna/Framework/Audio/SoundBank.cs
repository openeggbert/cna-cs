namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible sound-bank facade.</summary>
public class SoundBank : IDisposable
{
    private readonly CNA.Audio.SoundBank _soundBank;
    private bool _isDisposed;

    public SoundBank(AudioEngine audioEngine, string filename)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        _soundBank = new CNA.Audio.SoundBank(audioEngine.Framework, filename);
    }

    ~SoundBank()
    {
        Dispose(false);
    }

    public bool IsInUse => _soundBank.IsInUse;

    public bool IsDisposed => _isDisposed || _soundBank.IsDisposed;

    public event EventHandler<EventArgs>? Disposing;

    public Cue GetCue(string name) => new(_soundBank.GetCue(name));

    public void PlayCue(string name) => _soundBank.PlayCue(name);

    public void PlayCue(string name, AudioListener listener, AudioEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(emitter);
        _soundBank.PlayCue(name, listener.ToFramework(), emitter.ToFramework());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _soundBank?.Dispose();
        if (disposing)
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
