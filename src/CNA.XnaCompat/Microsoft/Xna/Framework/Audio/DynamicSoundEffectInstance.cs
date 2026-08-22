namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible procedural sound instance. Publicly sealed and derived from this
/// namespace's <see cref="SoundEffectInstance"/>; dynamic C ABI calls share the one handle owned by
/// that base.</summary>
public sealed class DynamicSoundEffectInstance : SoundEffectInstance
{
    private CNA.NativeEventBridge? _bufferNeededBridge;
    private EventHandler<EventArgs>? _bufferNeeded;
    private bool _disposed;

    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        : base(CNA.Audio.DynamicSoundEffectInstance.CreateNative(
            sampleRate,
            (CNA.Audio.AudioChannels)(int)channels))
    {
    }

    public override bool IsLooped
    {
        get => base.IsLooped;
        set => base.IsLooped = value;
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
        ArgumentNullException.ThrowIfNull(buffer);
        SubmitBuffer(buffer, 0, buffer.Length);
    }

    public void SubmitBuffer(byte[] buffer, int offset, int count) =>
        CNA.Audio.DynamicSoundEffectInstance.SubmitBuffer(
            CompatibilityHandle, this, buffer, offset, count);

    public TimeSpan GetSampleDuration(int sizeInBytes) =>
        CNA.Audio.DynamicSoundEffectInstance.QuerySampleDuration(
            CompatibilityHandle, this, sizeInBytes);

    public int GetSampleSizeInBytes(TimeSpan duration) =>
        CNA.Audio.DynamicSoundEffectInstance.QuerySampleSizeInBytes(
            CompatibilityHandle, this, duration);

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
