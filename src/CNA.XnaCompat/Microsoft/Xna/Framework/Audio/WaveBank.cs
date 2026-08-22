namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible wave-bank facade.</summary>
public class WaveBank : IDisposable
{
    private readonly CNA.Audio.WaveBank _waveBank;
    private bool _isDisposed;

    public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        _waveBank = new CNA.Audio.WaveBank(audioEngine.Framework, nonStreamingWaveBankFilename);
    }

    public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetsize)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        _waveBank = new CNA.Audio.WaveBank(audioEngine.Framework, streamingWaveBankFilename, offset, packetsize);
    }

    ~WaveBank()
    {
        Dispose(false);
    }

    public bool IsInUse => _waveBank.IsInUse;

    public bool IsPrepared => _waveBank.IsPrepared;

    public bool IsDisposed => _isDisposed || _waveBank.IsDisposed;

    public event EventHandler<EventArgs>? Disposing;

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
        _waveBank?.Dispose();
        if (disposing)
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
