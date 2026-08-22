namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible sound-effect facade backed by one CNA sound resource.</summary>
public sealed class SoundEffect : IDisposable
{
    private readonly CNA.Audio.SoundEffect _soundEffect;
    private bool _disposed;

    public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
    {
        _soundEffect = new CNA.Audio.SoundEffect(buffer, sampleRate, (CNA.Audio.AudioChannels)(int)channels);
    }

    public SoundEffect(
        byte[] buffer,
        int offset,
        int count,
        int sampleRate,
        AudioChannels channels,
        int loopStart,
        int loopLength)
    {
        _soundEffect = new CNA.Audio.SoundEffect(
            buffer, offset, count, sampleRate, (CNA.Audio.AudioChannels)(int)channels, loopStart, loopLength);
    }

    internal SoundEffect(nint nativeHandleValue)
    {
        _soundEffect = new CNA.Audio.SoundEffect(nativeHandleValue);
    }

    ~SoundEffect()
    {
        Dispose(false);
    }

    public bool IsDisposed => _disposed || _soundEffect.IsDisposed;

    public string Name
    {
        get => _soundEffect.Name;
        set => _soundEffect.Name = value;
    }

    public TimeSpan Duration => _soundEffect.Duration;

    public static float MasterVolume
    {
        get => CNA.Audio.SoundEffect.MasterVolume;
        set => CNA.Audio.SoundEffect.MasterVolume = value;
    }

    public static float SpeedOfSound
    {
        get => CNA.Audio.SoundEffect.SpeedOfSound;
        set => CNA.Audio.SoundEffect.SpeedOfSound = value;
    }

    public static float DopplerScale
    {
        get => CNA.Audio.SoundEffect.DopplerScale;
        set => CNA.Audio.SoundEffect.DopplerScale = value;
    }

    public static float DistanceScale
    {
        get => CNA.Audio.SoundEffect.DistanceScale;
        set => CNA.Audio.SoundEffect.DistanceScale = value;
    }

    public static SoundEffect FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using CNA.Audio.SoundEffect decoded = CNA.Audio.SoundEffect.FromStream(stream);
        return new SoundEffect(decoded.DetachNativeHandle());
    }

    public SoundEffectInstance CreateInstance() => new(_soundEffect.CreateNativeInstanceHandle());

    public bool Play() => _soundEffect.Play();

    public bool Play(float volume, float pitch, float pan) => _soundEffect.Play(volume, pitch, pan);

    public static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate, AudioChannels channels) =>
        CNA.Audio.SoundEffect.GetSampleDuration(sizeInBytes, sampleRate, (CNA.Audio.AudioChannels)(int)channels);

    public static int GetSampleSizeInBytes(TimeSpan duration, int sampleRate, AudioChannels channels) =>
        CNA.Audio.SoundEffect.GetSampleSizeInBytes(duration, sampleRate, (CNA.Audio.AudioChannels)(int)channels);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        _ = disposing;
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _soundEffect?.Dispose();
    }
}
