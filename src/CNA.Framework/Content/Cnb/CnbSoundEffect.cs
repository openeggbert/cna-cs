using CNA.Audio;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>How a compiled CNB sound's samples are stored.</summary>
public enum CnbAudioFormat
{
    Unknown = 0,
    Pcm16 = 1,
    Pcm8 = 2,
    PcmFloat32 = 3,
    Adpcm = 4,
    Vorbis = 5,
}

/// <summary>
/// A decoded CNB sound effect: the sample bytes and everything needed to interpret them.
///
/// The same split as the other CNB slices -- this is a description, readable without an audio
/// engine, and <see cref="CnbSoundEffectLoader"/> is the step that produces a playable
/// <see cref="SoundEffect"/>.
///
/// <b>Ownership.</b> The description handle is owned and destroyed here. <see cref="Samples"/> is a
/// copy taken once at decode time, because CNA's accessor writes into a caller buffer -- so nothing
/// here is a view into memory this object will free.
/// </summary>
public sealed class CnbSoundEffect : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly CnaCnbSoundEffectInfo _info;

    private CnbSoundEffect(nint handleValue, CnaCnbSoundEffectInfo info, byte[] samples)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_sound_effect_data_destroy(new CnaHandle(h)).IsSuccess());
        _info = info;
        Samples = samples;
    }

    /// <summary>Decodes the sound a container holds.</summary>
    /// <exception cref="CnaException">The document is not a sound effect, or its declared counts and
    /// payload length disagree.</exception>
    public static unsafe CnbSoundEffect Decode(CnbDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CnaResult result = Native.cna_cnb_decode_sound_effect(document.NativeHandle, out CnaHandle sound);
        CnaException.ThrowIfFailed(result, nameof(Decode));
        GC.KeepAlive(document);

        try
        {
            var info = CnaCnbSoundEffectInfo.Versioned();
            CnaException.ThrowIfFailed(
                Native.cna_cnb_sound_effect_data_get_info(sound, ref info), nameof(Decode));

            // BufferTooSmall is the documented answer to a zero-capacity size query on the
            // capacity-probe routes, not a failure -- the same convention the CNB model read path
            // met and got wrong first.
            CnaResult sizeResult = Native.cna_cnb_sound_effect_data_copy_samples(
                sound, null, 0, out ulong byteCount);
            if (sizeResult != CnaResult.BufferTooSmall)
            {
                CnaException.ThrowIfFailed(sizeResult, nameof(Decode));
            }

            var samples = new byte[checked((int)byteCount)];
            if (samples.Length > 0)
            {
                fixed (byte* destination = samples)
                {
                    CnaException.ThrowIfFailed(
                        Native.cna_cnb_sound_effect_data_copy_samples(
                            sound, destination, byteCount, out _),
                        nameof(Decode));
                }
            }

            return new CnbSoundEffect(sound.AsNint, info, samples);
        }
        catch
        {
            // Ours the moment the decode succeeded, so it is released even though construction
            // never completed.
            _ = Native.cna_cnb_sound_effect_data_destroy(sound);
            throw;
        }
    }

    /// <summary>Opens a <c>.cnb</c> file and decodes the sound in it.</summary>
    public static CnbSoundEffect DecodeFile(string path)
    {
        using CnbDocument document = CnbDocument.Open(path);
        return Decode(document);
    }

    /// <summary>How the samples are stored.</summary>
    public CnbAudioFormat Format => (CnbAudioFormat)_info.Format;

    public int SampleRate => checked((int)_info.SampleRate);

    public int Channels => checked((int)_info.Channels);

    /// <summary>Frames, not samples: a frame is one sample per channel.</summary>
    public int FrameCount => checked((int)_info.FrameCount);

    /// <summary>First frame of the loop region.</summary>
    public int LoopStart => checked((int)_info.LoopStart);

    /// <summary>Length of the loop region in frames; zero when the sound does not loop.</summary>
    public int LoopLength => checked((int)_info.LoopLength);

    /// <summary>The headerless sample bytes, copied at decode time.</summary>
    public byte[] Samples { get; }

    public void Dispose() => _handle.Dispose();
}

/// <summary>
/// Turns a decoded <see cref="CnbSoundEffect"/> into a playable <see cref="SoundEffect"/>.
///
/// <b>Only PCM16 crosses.</b> <see cref="SoundEffect"/>'s buffer constructor is XNA's, and XNA's
/// takes 16-bit PCM -- so an ADPCM or Vorbis sound is refused here by name rather than handed over
/// as bytes that would be played as noise.
///
/// <b>That refusal is unreachable today, and the comment says so rather than implying a test.</b>
/// CNB schema 1 stores PCM16 only: the other identifiers are reserved with no codec in this build,
/// and CNA's own encoder refuses to author one -- which is what
/// <c>ANonPcm16Sound_CannotBeAuthored</c> pins instead. The guard stays because the identifiers are
/// reserved rather than absent, so a schema that adds a codec would otherwise reach the PCM16
/// constructor with bytes that are not PCM16.
/// </summary>
public static class CnbSoundEffectLoader
{
    /// <summary>Builds a playable sound from a decoded description.</summary>
    /// <exception cref="NotSupportedException">The sound is not PCM16.</exception>
    public static SoundEffect Load(CnbSoundEffect sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        if (sound.Format != CnbAudioFormat.Pcm16)
        {
            throw new NotSupportedException(
                $"This CNB sound is {sound.Format}; SoundEffect's buffer constructor is XNA's and " +
                "takes 16-bit PCM. Decode it yourself, or ask CNA for a PCM16 build of the asset.");
        }

        if (sound.Channels is not (1 or 2))
        {
            throw new NotSupportedException(
                $"This CNB sound has {sound.Channels} channels; XNA's SoundEffect takes mono or stereo.");
        }

        return new SoundEffect(
            sound.Samples,
            sound.SampleRate,
            sound.Channels == 1 ? AudioChannels.Mono : AudioChannels.Stereo);
    }

    /// <summary>Opens a <c>.cnb</c> file and builds the sound it holds.</summary>
    public static SoundEffect LoadSoundEffect(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using CnbSoundEffect sound = CnbSoundEffect.DecodeFile(path);
        return Load(sound);
    }
}
