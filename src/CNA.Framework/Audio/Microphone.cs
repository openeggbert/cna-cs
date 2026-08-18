using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>Microphone</c>: a capture device, read by polling
/// <see cref="GetData(byte[])"/> between <see cref="Start"/> and <see cref="Stop"/>.
///
/// Like <see cref="Graphics.GraphicsAdapter"/>, and for the same reason, this holds an
/// <see cref="Index"/> rather than a handle: every native microphone call is <c>*_at(game,
/// index)</c>, because microphones are entries in a list the runtime enumerates rather than
/// resources the caller owns. Nothing here needs disposing.
///
/// That also forces the one shape difference from XNA: <c>Microphone.All</c> and
/// <c>Microphone.Default</c> are static there, but enumerating needs the game handle, so
/// <see cref="All"/> and <see cref="Default"/> read the ambient game the way
/// <c>Keyboard</c>/<c>Mouse</c>/<c>TouchPanel</c> do -- which keeps them static after all.
///
/// Instances are cached per index rather than built fresh on every read of <see cref="All"/>. That
/// is not an optimisation: <see cref="BufferReady"/> holds a native subscription, and a transient
/// wrapper would make <c>Microphone.Default.BufferReady += h</c> subscribe an object that is
/// garbage the next statement, leaking the registration and never raising anything the caller could
/// observe. Caching also gives the reference equality XNA callers already assume when they compare
/// a stored microphone against <see cref="Default"/>.
/// </summary>
public class Microphone
{
    /// <summary>The per-index instance cache -- see this class's own doc comment for why it exists.
    /// Guarded by itself; the microphone list is read from whichever thread asks.</summary>
    private static readonly Dictionary<ulong, Microphone> Instances = [];

    /// <summary>Guards this instance's subscription state. Deliberately *not* the
    /// <see cref="Instances"/> lock: the native subscribe call happens under it, and the header says
    /// the callback comes from whichever thread advances capture -- so holding the cache lock across
    /// that call would let a handler that reads <see cref="Default"/> deadlock against the thread
    /// subscribing.</summary>
    private readonly object _bufferReadyLock = new();

    private NativeEventBridge? _bufferReadyBridge;
    private EventHandler<EventArgs>? _bufferReady;

    private Microphone(ulong index)
    {
        Index = index;
    }

    public ulong Index { get; }

    /// <summary>Returns the cached instance for <paramref name="index"/>, creating it on first
    /// ask.</summary>
    private static Microphone Get(ulong index)
    {
        lock (Instances)
        {
            if (!Instances.TryGetValue(index, out Microphone? microphone))
            {
                microphone = new Microphone(index);
                Instances[index] = microphone;
            }

            return microphone;
        }
    }

    /// <summary>
    /// Raised when captured audio is ready to read with <see cref="GetData(byte[])"/>. Matches real
    /// XNA's <c>BufferReady</c>, and is an alternative to polling, not a replacement -- XNA offers
    /// both and so does this.
    ///
    /// Subscribing takes a native registration on first <c>+=</c> and holds it until the game is
    /// disposed. It cannot be released on the last <c>-=</c>, because a microphone is not a
    /// disposable resource in XNA's API and there is no other moment a caller could hand back.
    /// <see cref="ReleaseAllSubscriptions"/> is what actually ends it, driven by
    /// <see cref="Game"/> disposal, since a registration outliving its game would leave native able
    /// to call into a freed context.
    ///
    /// Raised from the capture thread, not necessarily the game thread. This binding does not
    /// marshal it, for the reason <see cref="DynamicSoundEffectInstance"/> records.
    /// </summary>
    public event EventHandler<EventArgs>? BufferReady
    {
        add
        {
            EnsureBufferReadySubscribed();
            _bufferReady += value;
        }
        remove => _bufferReady -= value;
    }

    private void EnsureBufferReadySubscribed()
    {
        lock (_bufferReadyLock)
        {
            if (_bufferReadyBridge is not null)
            {
                return;
            }

            _bufferReadyBridge = NativeEventBridge.Subscribe(
                () => _bufferReady?.Invoke(this, EventArgs.Empty),
                (callback, context) =>
                {
                    CnaResult result = Native.cna_microphone_subscribe_buffer_ready_at(
                        CnaAmbientGame.Current, Index, callback, context, out CnaHandle registration);
                    CnaException.ThrowIfFailed(result, nameof(BufferReady));
                    return registration;
                },
                registration => Native.cna_audio_unsubscribe_ext(registration));
        }
    }

    /// <summary>Releases every <see cref="BufferReady"/> registration and drops the instance cache.
    /// Called from <see cref="Game"/>'s disposal: registrations are taken against that game, so
    /// leaving one alive past it would leave native holding a context pointer into a freed
    /// <see cref="System.Runtime.InteropServices.GCHandle"/>. Handler failures captured but never
    /// surfaced are rethrown afterwards -- the first one, with the rest counted -- rather than lost
    /// with the cache.</summary>
    internal static void ReleaseAllSubscriptions()
    {
        Microphone[] cached;
        lock (Instances)
        {
            cached = [.. Instances.Values];
            Instances.Clear();
        }

        Exception? pending = null;
        foreach (Microphone microphone in cached)
        {
            NativeEventBridge? bridge;
            lock (microphone._bufferReadyLock)
            {
                bridge = microphone._bufferReadyBridge;
                microphone._bufferReadyBridge = null;
                microphone._bufferReady = null;
            }

            if (bridge is null)
            {
                continue;
            }

            try
            {
                bridge.ThrowPendingException();
            }
            catch (Exception ex)
            {
                pending ??= ex;
            }

            bridge.Dispose();
        }

        if (pending is not null)
        {
            throw pending;
        }
    }

    public unsafe string Name => NativeStringReader.ReadIndexed(
        Native.cna_microphone_get_name_size_at, Native.cna_microphone_copy_name_at, CnaAmbientGame.Current, Index, nameof(Name));

    public bool IsHeadset
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_is_headset_at(CnaAmbientGame.Current, Index, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsHeadset));
            return value != 0;
        }
    }

    public int SampleRate
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_sample_rate_at(CnaAmbientGame.Current, Index, out int value);
            CnaException.ThrowIfFailed(result, nameof(SampleRate));
            return value;
        }
    }

    public MicrophoneState State
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_state_at(CnaAmbientGame.Current, Index, out uint value);
            CnaException.ThrowIfFailed(result, nameof(State));
            return (MicrophoneState)value;
        }
    }

    /// <summary>How much audio the device buffers before <see cref="GetData(byte[])"/> has
    /// something to hand back. Settable, matching XNA -- a shorter buffer means lower latency and
    /// more frequent polling.</summary>
    public TimeSpan BufferDuration
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_buffer_duration_ticks_at(CnaAmbientGame.Current, Index, out long ticks);
            CnaException.ThrowIfFailed(result, nameof(BufferDuration));
            return TimeSpan.FromTicks(ticks);
        }
        set
        {
            CnaResult result = Native.cna_microphone_set_buffer_duration_ticks_at(CnaAmbientGame.Current, Index, value.Ticks);
            CnaException.ThrowIfFailed(result, nameof(BufferDuration));
        }
    }

    public static IReadOnlyList<Microphone> All
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_count(CnaAmbientGame.Current, out ulong count);
            CnaException.ThrowIfFailed(result, nameof(All));

            var microphones = new Microphone[count];
            for (ulong i = 0; i < count; i++)
            {
                microphones[i] = Get(i);
            }

            return microphones;
        }
    }

    /// <summary><see langword="null"/> when the machine has no capture device -- the native call
    /// reports availability separately from the index precisely so that is distinguishable, rather
    /// than making index zero mean "none".</summary>
    public static Microphone? Default
    {
        get
        {
            CnaResult result = Native.cna_microphone_get_default_index_ext(CnaAmbientGame.Current, out ulong index, out byte available);
            CnaException.ThrowIfFailed(result, nameof(Default));
            return available != 0 ? Get(index) : null;
        }
    }

    public void Start()
    {
        CnaResult result = Native.cna_microphone_start_at(CnaAmbientGame.Current, Index);
        CnaException.ThrowIfFailed(result, nameof(Start));
    }

    public void Stop()
    {
        CnaResult result = Native.cna_microphone_stop_at(CnaAmbientGame.Current, Index);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    /// <summary>Copies captured audio into <paramref name="buffer"/> and returns how many bytes
    /// were written -- which may be fewer than the buffer holds, or zero when nothing has been
    /// captured yet.</summary>
    public unsafe int GetData(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return GetData(buffer, 0, buffer.Length);
    }

    public unsafe int GetData(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

        if (count == 0)
        {
            return 0;
        }

        fixed (byte* bufferPtr = &buffer[offset])
        {
            CnaResult result = Native.cna_microphone_get_data_at(
                CnaAmbientGame.Current, Index, bufferPtr, (ulong)count, out ulong written);
            CnaException.ThrowIfFailed(result, nameof(GetData));
            return (int)written;
        }
    }

    public TimeSpan GetSampleDuration(int sizeInBytes)
    {
        CnaResult result = Native.cna_microphone_get_sample_duration_ticks_at(
            CnaAmbientGame.Current, Index, sizeInBytes, out long ticks);
        CnaException.ThrowIfFailed(result, nameof(GetSampleDuration));
        return TimeSpan.FromTicks(ticks);
    }

    public int GetSampleSizeInBytes(TimeSpan duration)
    {
        CnaResult result = Native.cna_microphone_get_sample_size_in_bytes_at(
            CnaAmbientGame.Current, Index, duration.Ticks, out int bytes);
        CnaException.ThrowIfFailed(result, nameof(GetSampleSizeInBytes));
        return bytes;
    }
}
