namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible procedural sound instance. Publicly sealed and derived from this
/// namespace's <see cref="SoundEffectInstance"/>; dynamic C ABI calls share the one handle owned by
/// that base.</summary>
public sealed class DynamicSoundEffectInstance : SoundEffectInstance
{
    private CNA.NativeEventBridge? _bufferNeededBridge;
    private EventHandler<EventArgs>? _bufferNeeded;
    private readonly int _sampleRate;
    private readonly AudioChannels _channels;
    private bool _disposed;

    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        : base(CNA.Audio.DynamicSoundEffectInstance.CreateNative(
            sampleRate,
            (CNA.Audio.AudioChannels)(int)channels))
    {
        _sampleRate = sampleRate;
        _channels = channels;
    }

    public override bool IsLooped
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return false;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (value)
            {
                throw new InvalidOperationException("A DynamicSoundEffectInstance cannot be looped.");
            }
        }
    }

    public int PendingBufferCount =>
        CNA.Audio.DynamicSoundEffectInstance.QueryPendingBufferCount(CompatibilityHandle, this);

    public event EventHandler<EventArgs>? BufferNeeded
    {
        add
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _bufferNeededBridge ??= CNA.Audio.DynamicSoundEffectInstance.SubscribeBufferNeeded(
                CompatibilityHandle,
                this,
                () => _bufferNeeded?.Invoke(this, EventArgs.Empty));
            _bufferNeeded += value;
        }
        remove => _bufferNeeded -= value;
    }

    public override void Play() => base.Play();

    public void SubmitBuffer(byte[] buffer)
    {
        // XNA reads Length before entering the range overload; null therefore has the observable
        // NullReferenceException ordering, rather than ArgumentNullException.
        SubmitBuffer(buffer, 0, buffer.Length);
    }

    public void SubmitBuffer(byte[] buffer, int offset, int count) =>
        SubmitBufferCore(buffer, offset, count);

    public TimeSpan GetSampleDuration(int sizeInBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (sizeInBytes < 0)
        {
            throw new ArgumentException("The audio buffer size cannot be negative.");
        }

        if (sizeInBytes == 0)
        {
            return TimeSpan.Zero;
        }

        int sampleCount = sizeInBytes / (2 * (int)_channels);
        float milliseconds = (float)sampleCount * 1000f / (float)_sampleRate;
        return TimeSpan.FromMilliseconds((double)milliseconds);
    }

    public int GetSampleSizeInBytes(TimeSpan duration)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        double milliseconds = duration.TotalMilliseconds;
        if (milliseconds < 0d || milliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        if (duration == TimeSpan.Zero)
        {
            return 0;
        }

        try
        {
            int sampleCount = checked((int)(milliseconds * ((float)_sampleRate / 1000f)));
            return checked((sampleCount + sampleCount % (int)_channels) * (2 * (int)_channels));
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }

    private void SubmitBufferCore(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        int blockAlign = 2 * (int)_channels;
        if (buffer is null || buffer.Length == 0 || buffer.Length % blockAlign != 0)
        {
            throw new ArgumentException("The audio buffer is invalid.");
        }

        if (offset < 0 || offset >= buffer.Length || offset % blockAlign != 0)
        {
            throw new ArgumentException("The audio buffer offset is invalid.");
        }

        int end;
        try
        {
            end = checked(offset + count);
        }
        catch (OverflowException)
        {
            throw new ArgumentException("The offset and count do not describe a valid buffer range.");
        }

        if (count <= 0 || end > buffer.Length || count % blockAlign != 0)
        {
            throw new ArgumentException("The offset and count do not describe a valid buffer range.");
        }

        CNA.Audio.DynamicSoundEffectInstance.SubmitBuffer(
            CompatibilityHandle, this, buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        Exception? pending = null;
        if (_bufferNeededBridge is CNA.NativeEventBridge bridge)
        {
            _bufferNeededBridge = null;
            try
            {
                bridge.ThrowPendingException();
            }
            catch (Exception exception)
            {
                pending = exception;
            }

            bridge.Dispose();
        }

        base.Dispose(disposing);
        if (pending is not null)
        {
            throw pending;
        }
    }
}
