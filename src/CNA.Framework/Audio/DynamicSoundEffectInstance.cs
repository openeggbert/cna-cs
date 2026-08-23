using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>DynamicSoundEffectInstance</c>: a sound instance the game feeds PCM into
/// at run time (procedural audio, streaming a decoder, network voice) rather than one created from
/// a loaded <see cref="SoundEffect"/>.
///
/// A real subclass of <see cref="SoundEffectInstance"/>, as in XNA -- <c>Play</c>/<c>Pause</c>/
/// <c>Stop</c>/<c>Volume</c>/<c>Pitch</c>/<c>Pan</c>/<c>State</c> are all inherited, and only the
/// buffer-submission surface is new.
///
/// <see cref="BufferNeeded"/> is real since WP15, over
/// <c>cna_dynamic_sound_effect_instance_subscribe_buffer_needed</c> and the shared
/// <see cref="NativeEventBridge"/>. It was deferred before that only because the pinned-context
/// lifetime machinery did not exist yet. <see cref="PendingBufferCount"/> remains available for
/// games that would rather poll from their fixed-step loop than be called back.
///
/// <b>The event is not necessarily raised on the game thread.</b> The C API says it comes from
/// "whichever thread advances the queue, which is the game thread when the loop runs"
/// (<c>audio.h:768-770</c>) -- so a handler that touches game state must not assume otherwise. This
/// binding does not marshal it onto the game thread, because doing so would delay the callback past
/// the point where feeding the queue still helps, which is the entire purpose of the event.
/// </summary>
public class DynamicSoundEffectInstance : SoundEffectInstance
{
    private NativeEventBridge? _bufferNeededBridge;
    private EventHandler<EventArgs>? _bufferNeeded;
    private bool _disposed;

    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        : base(CreateNative(sampleRate, channels))
    {
    }

    internal static nint CreateNative(int sampleRate, AudioChannels channels)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_create(
            CnaAmbientGame.Current, sampleRate, (uint)channels, out CnaHandle instance);
        CnaException.ThrowIfFailed(result, nameof(DynamicSoundEffectInstance));
        return instance.AsNint;
    }

    /// <summary>
    /// Raised when the queue runs low and the game should submit more audio. Matches real XNA's
    /// <c>BufferNeeded</c>.
    ///
    /// The native subscription is taken on the first <c>+=</c> and held until disposal rather than
    /// dropped on the last <c>-=</c> -- see <see cref="GraphicsDeviceManager.DeviceCreated"/> for
    /// why. Read this class's own doc comment for the threading caveat before touching game state
    /// from a handler.
    /// </summary>
    public event EventHandler<EventArgs>? BufferNeeded
    {
        add
        {
            EnsureBufferNeededSubscribed();
            _bufferNeeded += value;
        }
        remove => _bufferNeeded -= value;
    }

    private void EnsureBufferNeededSubscribed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_bufferNeededBridge is not null)
        {
            return;
        }

        _bufferNeededBridge = SubscribeBufferNeeded(
            NativeHandleValue, this, () => _bufferNeeded?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Unsubscribes before the base class releases the instance handle the registration is
    /// taken against -- the other order would leave native able to call back into a context whose
    /// owner is gone. Any handler failure captured but never surfaced is rethrown last, so a game
    /// that only ever submits buffers still hears about it.</summary>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;

        Exception? pending = null;
        if (_bufferNeededBridge is NativeEventBridge bridge)
        {
            _bufferNeededBridge = null;

            try
            {
                bridge.ThrowPendingException();
            }
            catch (Exception ex)
            {
                pending = ex;
            }

            bridge.Dispose();
        }

        base.Dispose(disposing);

        if (pending is not null)
        {
            throw pending;
        }
    }

    /// <summary>How many submitted buffers have not been consumed yet. A game submits more while
    /// this is low; when it reaches zero the instance has run dry and will stop.</summary>
    public int PendingBufferCount
        => QueryPendingBufferCount(NativeHandleValue, this);

    public unsafe void SubmitBuffer(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        SubmitBuffer(buffer, 0, buffer.Length);
    }

    public unsafe void SubmitBuffer(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

        fixed (byte* bufferPtr = buffer)
        {
            // The native call takes the whole array plus an offset/count window, rather than a
            // pre-offset pointer -- so the pointer is to element zero, not to `offset`.
            CnaResult result = Native.cna_dynamic_sound_effect_instance_submit_buffer(
                new CnaHandle(NativeHandleValue), bufferPtr, (ulong)buffer.Length, offset, count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SubmitBuffer));
        }
    }

    public TimeSpan GetSampleDuration(int sizeInBytes)
        => QuerySampleDuration(NativeHandleValue, this, sizeInBytes);

    public int GetSampleSizeInBytes(TimeSpan duration)
        => QuerySampleSizeInBytes(NativeHandleValue, this, duration);

    internal static NativeEventBridge SubscribeBufferNeeded(
        nint nativeHandleValue,
        object lifetimeOwner,
        Action dispatch) =>
        NativeEventBridge.Subscribe(
            dispatch,
            (callback, context) =>
            {
                CnaResult result = Native.cna_dynamic_sound_effect_instance_subscribe_buffer_needed(
                    new CnaHandle(nativeHandleValue), callback, context, out CnaHandle registration);
                GC.KeepAlive(lifetimeOwner);
                CnaException.ThrowIfFailed(result, nameof(BufferNeeded));
                return registration;
            },
            registration => Native.cna_audio_unsubscribe_ext(registration));

    internal static int QueryPendingBufferCount(nint nativeHandleValue, object lifetimeOwner)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_get_pending_buffer_count(
            new CnaHandle(nativeHandleValue), out int value);
        GC.KeepAlive(lifetimeOwner);
        CnaException.ThrowIfFailed(result, nameof(PendingBufferCount));
        return value;
    }

    internal static unsafe void SubmitBuffer(
        nint nativeHandleValue,
        object lifetimeOwner,
        byte[] buffer,
        int offset,
        int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

        fixed (byte* bufferPtr = buffer)
        {
            CnaResult result = Native.cna_dynamic_sound_effect_instance_submit_buffer(
                new CnaHandle(nativeHandleValue), bufferPtr, (ulong)buffer.Length, offset, count);
            GC.KeepAlive(lifetimeOwner);
            CnaException.ThrowIfFailed(result, nameof(SubmitBuffer));
        }
    }

    internal static TimeSpan QuerySampleDuration(nint nativeHandleValue, object lifetimeOwner, int sizeInBytes)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_get_sample_duration_ticks(
            new CnaHandle(nativeHandleValue), sizeInBytes, out long ticks);
        GC.KeepAlive(lifetimeOwner);
        CnaException.ThrowIfFailed(result, nameof(GetSampleDuration));
        return TimeSpan.FromTicks(ticks);
    }

    internal static int QuerySampleSizeInBytes(nint nativeHandleValue, object lifetimeOwner, TimeSpan duration)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_get_sample_size_in_bytes(
            new CnaHandle(nativeHandleValue), duration.Ticks, out int bytes);
        GC.KeepAlive(lifetimeOwner);
        CnaException.ThrowIfFailed(result, nameof(GetSampleSizeInBytes));
        return bytes;
    }
}
