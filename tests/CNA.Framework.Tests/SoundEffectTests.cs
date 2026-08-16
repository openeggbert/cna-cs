using CNA.Audio;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// SoundEffect.GetSampleDuration/GetSampleSizeInBytes are pure arithmetic (see
/// ../../src/CNA.Framework/Audio/SoundEffect.cs) -- no native dependency, so these run without a
/// real cna-native, unlike SoundEffect's own constructors (which create a native resource
/// immediately, so they're untestable here the same way Texture2D's constructor is).
/// </summary>
public class SoundEffectTests
{
    [Fact]
    public void GetSampleDuration_ZeroBytes_ReturnsZero()
    {
        Assert.Equal(TimeSpan.Zero, SoundEffect.GetSampleDuration(0, 44100, AudioChannels.Stereo));
    }

    [Fact]
    public void GetSampleDuration_OneSecondOfMonoAudio_ComputesCorrectly()
    {
        // 44100 samples/sec * 2 bytes/sample (16-bit PCM) * 1 channel = 88200 bytes for one second.
        TimeSpan duration = SoundEffect.GetSampleDuration(88200, 44100, AudioChannels.Mono);

        Assert.Equal(TimeSpan.FromSeconds(1), duration);
    }

    [Fact]
    public void GetSampleDuration_StereoDoublesBytesPerSecond()
    {
        // Same byte count as the mono test above, but stereo halves the duration (2x bytes/sample).
        TimeSpan duration = SoundEffect.GetSampleDuration(88200, 44100, AudioChannels.Stereo);

        Assert.Equal(TimeSpan.FromSeconds(0.5), duration);
    }

    [Fact]
    public void GetSampleSizeInBytes_OneSecondOfMonoAudio_ComputesCorrectly()
    {
        int size = SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(1), 44100, AudioChannels.Mono);

        Assert.Equal(88200, size);
    }

    [Theory]
    [InlineData(44100, AudioChannels.Mono)]
    [InlineData(44100, AudioChannels.Stereo)]
    [InlineData(22050, AudioChannels.Mono)]
    [InlineData(48000, AudioChannels.Stereo)]
    public void GetSampleSizeInBytes_AndGetSampleDuration_RoundTrip(int sampleRate, AudioChannels channels)
    {
        var original = TimeSpan.FromSeconds(2.5);

        int sizeInBytes = SoundEffect.GetSampleSizeInBytes(original, sampleRate, channels);
        TimeSpan roundTripped = SoundEffect.GetSampleDuration(sizeInBytes, sampleRate, channels);

        // Rounds to whole PCM samples, so allow up to one sample period of drift.
        double samplePeriodSeconds = 1.0 / sampleRate;
        Assert.True(
            Math.Abs((roundTripped - original).TotalSeconds) <= samplePeriodSeconds,
            $"original={original}, roundTripped={roundTripped}");
    }

    [Fact]
    public void GetSampleDuration_NegativeSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundEffect.GetSampleDuration(-1, 44100, AudioChannels.Mono));
    }

    [Fact]
    public void GetSampleDuration_NonPositiveSampleRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundEffect.GetSampleDuration(100, 0, AudioChannels.Mono));
    }

    [Fact]
    public void GetSampleSizeInBytes_NegativeDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(-1), 44100, AudioChannels.Mono));
    }
}
