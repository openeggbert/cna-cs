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
/// </summary>
public class Microphone
{
    private Microphone(ulong index)
    {
        Index = index;
    }

    public ulong Index { get; }

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
                microphones[i] = new Microphone(i);
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
            return available != 0 ? new Microphone(index) : null;
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
