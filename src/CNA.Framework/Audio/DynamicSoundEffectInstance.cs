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
/// <c>BufferNeeded</c> is deliberately absent. The C API has
/// <c>cna_dynamic_sound_effect_instance_subscribe_buffer_needed</c>, but wiring a native callback
/// into a managed event needs the same pinned-context lifetime machinery <see cref="Game"/> uses
/// for its lifecycle callbacks, and that is its own increment rather than something to bolt on
/// here -- tracked in <c>plan.md</c> WP15. <see cref="PendingBufferCount"/> is polled instead,
/// which is what a fixed-step game loop does anyway.
/// </summary>
public class DynamicSoundEffectInstance : SoundEffectInstance
{
    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        : base(CreateNative(sampleRate, channels))
    {
    }

    private static nint CreateNative(int sampleRate, AudioChannels channels)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_create(
            CnaAmbientGame.Current, sampleRate, (uint)channels, out CnaHandle instance);
        CnaException.ThrowIfFailed(result, nameof(DynamicSoundEffectInstance));
        return instance.AsNint;
    }

    /// <summary>How many submitted buffers have not been consumed yet. A game submits more while
    /// this is low; when it reaches zero the instance has run dry and will stop.</summary>
    public int PendingBufferCount
    {
        get
        {
            CnaResult result = Native.cna_dynamic_sound_effect_instance_get_pending_buffer_count(
                new CnaHandle(NativeHandleValue), out int value);
            CnaException.ThrowIfFailed(result, nameof(PendingBufferCount));
            return value;
        }
    }

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
            CnaException.ThrowIfFailed(result, nameof(SubmitBuffer));
        }
    }

    public TimeSpan GetSampleDuration(int sizeInBytes)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_get_sample_duration_ticks(
            new CnaHandle(NativeHandleValue), sizeInBytes, out long ticks);
        CnaException.ThrowIfFailed(result, nameof(GetSampleDuration));
        return TimeSpan.FromTicks(ticks);
    }

    public int GetSampleSizeInBytes(TimeSpan duration)
    {
        CnaResult result = Native.cna_dynamic_sound_effect_instance_get_sample_size_in_bytes(
            new CnaHandle(NativeHandleValue), duration.Ticks, out int bytes);
        CnaException.ThrowIfFailed(result, nameof(GetSampleSizeInBytes));
        return bytes;
    }
}
