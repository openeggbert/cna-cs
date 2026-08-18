namespace Microsoft.Xna.Framework.Audio;

/// <summary>
/// XNA 4.0-compatible <c>SoundEffect</c>. <c>Duration</c>/<c>Dispose</c> are inherited unchanged
/// from <see cref="CNA.Audio.SoundEffect"/>. <c>AudioChannels</c> is a distinct enum type from
/// <see cref="CNA.Audio.AudioChannels"/> (enums cannot define conversion operators), so every
/// constructor/static method taking it needs an explicit cast at the boundary, and
/// <see cref="CreateInstance"/> needs a `new` override since its return type
/// (<see cref="SoundEffectInstance"/>) differs the same way <c>BoundingFrustum.GetCorners()</c>
/// already does.
/// </summary>
public class SoundEffect : CNA.Audio.SoundEffect
{
    public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
        : base(buffer, sampleRate, (CNA.Audio.AudioChannels)(int)channels)
    {
    }

    public SoundEffect(
        byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        : base(buffer, offset, count, sampleRate, (CNA.Audio.AudioChannels)(int)channels, loopStart, loopLength)
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    protected internal SoundEffect(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    public new SoundEffectInstance CreateInstance() => new(CreateNativeInstanceHandle());

    public static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate, AudioChannels channels) =>
        CNA.Audio.SoundEffect.GetSampleDuration(sizeInBytes, sampleRate, (CNA.Audio.AudioChannels)(int)channels);

    public static int GetSampleSizeInBytes(TimeSpan duration, int sampleRate, AudioChannels channels) =>
        CNA.Audio.SoundEffect.GetSampleSizeInBytes(duration, sampleRate, (CNA.Audio.AudioChannels)(int)channels);

    /// <summary>Matches real XNA's <c>SoundEffect.FromStream</c>, re-typed to this namespace's own
    /// <see cref="SoundEffect"/>. Cannot forward to the base factory -- that builds a
    /// <c>CNA.Audio.SoundEffect</c> and a compat effect is a separate class -- so it goes through
    /// the same <c>protected internal</c> raw-handle constructor <c>ContentManager</c> uses, taking
    /// ownership of the decoded handle.</summary>
    public static new SoundEffect FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using CNA.Audio.SoundEffect decoded = CNA.Audio.SoundEffect.FromStream(stream);
        return new SoundEffect(decoded.DetachNativeHandle());
    }
}
