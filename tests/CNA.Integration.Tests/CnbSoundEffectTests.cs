using CNA.Audio;
using CNA.Content.Cnb;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// A compiled CNB sound, decoded and then made playable.
///
/// The pair to <see cref="CnbSpriteFontTests"/>, and the last of the asset kinds a game reaches for
/// first. It needs no new audio routes on the other side: a decoded PCM16 sound is samples, a rate
/// and a channel count, which is exactly what <see cref="SoundEffect"/>'s public constructor takes.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbSoundEffectTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>Four stereo frames of 16-bit PCM, every sample distinct so a reader that
    /// transposed channels or halved the frame count has somewhere to go wrong.</summary>
    private static readonly byte[] StereoSamples =
    [
        0x01, 0x00, 0x02, 0x00,
        0x03, 0x00, 0x04, 0x00,
        0x05, 0x00, 0x06, 0x00,
        0x07, 0x00, 0x08, 0x00,
    ];

    private static string WritePcm16(
        int sampleRate = 22050, int channels = 2, int loopStart = 1, int loopLength = 2)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-sound-{Guid.NewGuid():N}.cnb");
        CnbTestSoundEffectWriter.Write(
            path, CnbAudioFormat.Pcm16, sampleRate, channels, StereoSamples.Length / (2 * channels),
            loopStart, loopLength, StereoSamples, "sounds/fixture");
        return path;
    }

    /// <summary>Every field of the description, and the samples byte for byte.</summary>
    [NativeFact]
    public void DecodedSound_CarriesItsShapeAndSamples()
    {
        string path = WritePcm16();
        try
        {
            using CnbSoundEffect sound = CnbSoundEffect.DecodeFile(path);

            Assert.Equal(CnbAudioFormat.Pcm16, sound.Format);
            Assert.Equal(22050, sound.SampleRate);
            Assert.Equal(2, sound.Channels);

            // Frames, not samples and not bytes. Sixteen bytes of stereo PCM16 is four frames, and
            // a reader reporting 8 or 16 here has confused one of the three.
            Assert.Equal(4, sound.FrameCount);

            // The loop region is two independent numbers; equal values would let a swap pass.
            Assert.Equal(1, sound.LoopStart);
            Assert.Equal(2, sound.LoopLength);

            Assert.Equal(StereoSamples, sound.Samples);
            output.WriteLine(
                $"{sound.Format} {sound.SampleRate}Hz x{sound.Channels}, {sound.FrameCount} frames, " +
                $"{sound.Samples.Length} bytes");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A mono sound reports one channel and twice the frames from the same bytes, which is
    /// what says the frame count is derived from the channel count rather than assumed.</summary>
    [NativeFact]
    public void DecodedSound_CountsFramesPerChannelLayout()
    {
        string stereo = WritePcm16(channels: 2);
        string mono = WritePcm16(channels: 1);
        try
        {
            using CnbSoundEffect asStereo = CnbSoundEffect.DecodeFile(stereo);
            using CnbSoundEffect asMono = CnbSoundEffect.DecodeFile(mono);

            Assert.Equal(4, asStereo.FrameCount);
            Assert.Equal(8, asMono.FrameCount);
            Assert.Equal(asStereo.Samples, asMono.Samples);
        }
        finally
        {
            File.Delete(stereo);
            File.Delete(mono);
        }
    }

    /// <summary>The whole point: a <c>.cnb</c> file becomes a playable <see cref="SoundEffect"/>
    /// whose duration follows from the frame count and rate the file declared.</summary>
    [NativeFact]
    public void CnbFile_BecomesAPlayableSoundEffect()
    {
        fixture.InsideAFrame(_ =>
        {
            string path = WritePcm16(sampleRate: 8000, channels: 1);
            try
            {
                using SoundEffect effect = CnbSoundEffectLoader.LoadSoundEffect(path);

                // 8 mono frames at 8 kHz is a millisecond. Asserted through Duration because that
                // is a property of the object CNA built, not of the bytes handed to it -- a rate
                // dropped on the way across would give a different one.
                output.WriteLine($"duration={effect.Duration.TotalMilliseconds:F3} ms");
                Assert.Equal(1.0, effect.Duration.TotalMilliseconds, 3);

                using SoundEffectInstance instance = effect.CreateInstance();
                Assert.NotNull(instance);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>
    /// A non-PCM16 sound cannot be authored at all: CNA's own encoder refuses it, by name.
    ///
    /// This test was written to check that <see cref="CnbSoundEffectLoader"/> refuses a format XNA's
    /// constructor cannot take, and could not be: **CNB schema 1 stores PCM16 only**, and the
    /// encoder says so -- "Adpcm is a reserved identifier with no codec in this build". So no
    /// CNA-authored file can carry one, the loader's guard is unreachable today, and the assertion
    /// moved to where the refusal actually happens.
    ///
    /// The guard stays, and its doc comment now says it is unreachable rather than implying it is
    /// tested. It costs nothing, the identifiers are reserved rather than absent, and a schema that
    /// adds a codec would otherwise hand ADPCM bytes to a PCM16 constructor and play noise.
    ///
    /// What this *does* pin is worth having on its own: every CNB sound a game meets today is
    /// PCM16, and a future build that changes that fails here.
    /// </summary>
    [NativeFact]
    public void ANonPcm16Sound_CannotBeAuthored()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-adpcm-{Guid.NewGuid():N}.cnb");
        try
        {
            CnaException failure = Assert.Throws<CnaException>(() => CnbTestSoundEffectWriter.Write(
                path, CnbAudioFormat.Adpcm, 22050, 1, 4, 0, 0, StereoSamples, "sounds/adpcm"));

            Assert.Equal("Io", failure.NativeResult);
            Assert.Contains("Adpcm", failure.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(path), "A refused encode must leave no file behind.");
            output.WriteLine($"the encoder refuses it: {failure.Message}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A document that is not a sound is refused rather than decoded into silence.</summary>
    [NativeFact]
    public void Decode_RefusesADocumentThatIsNotASound()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-notasound-{Guid.NewGuid():N}.cnb");
        try
        {
            using (var writer = new CnbTestWriter(0x54534554, 1))
            {
                writer.AddChunk(CnbTestWriter.ChunkId("ONE_"), [1, 2, 3, 4]);
                writer.WriteToFile(path);
            }

            using CnbDocument document = CnbDocument.Open(path);
            CnaException failure = Assert.Throws<CnaException>(() => CnbSoundEffect.Decode(document));
            output.WriteLine($"non-sound document refused: {failure.Message}");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
