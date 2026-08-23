using CNA.Audio;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// SoundEffect.GetSampleDuration/GetSampleSizeInBytes are pure arithmetic (see
/// ../../src/CNA.Framework/Audio/SoundEffect.cs) -- no native dependency, so these run without a
/// real cna-native, unlike SoundEffect's own constructors (which create a native resource
/// immediately, so they're untestable here the same way Texture2D's constructor is) *on their
/// success path*. Their argument-validation failure paths are testable, though: validation runs
/// before the native call, so a constructor call with bad arguments throws in pure managed code
/// and never reaches native at all -- see the Constructor_* tests below.
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

        // XNA converts through float32 milliseconds before truncating to a sample count.
        Assert.Equal(88198, size);
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

        // XNA converts through float32 in both directions; allow two sample periods of drift.
        double samplePeriodSeconds = 2.0 / sampleRate;
        Assert.True(
            Math.Abs((roundTripped - original).TotalSeconds) <= samplePeriodSeconds,
            $"original={original}, roundTripped={roundTripped}");
    }

    [Fact]
    public void GetSampleDuration_NegativeSize_Throws()
    {
        Assert.Throws<ArgumentException>(() => SoundEffect.GetSampleDuration(-1, 44100, AudioChannels.Mono));
    }

    [Fact]
    public void GetSampleDuration_NonPositiveSampleRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundEffect.GetSampleDuration(100, 0, AudioChannels.Mono));
    }

    [Fact]
    public void GetSampleSizeInBytes_NegativeDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(-1), 44100, AudioChannels.Mono));
    }

    [Fact]
    public void GetSampleDuration_UndefinedChannelsValue_ThrowsInsteadOfDividingByZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SoundEffect.GetSampleDuration(100, 44100, (AudioChannels)0));
    }

    [Fact]
    public void GetSampleSizeInBytes_UndefinedChannelsValue_ThrowsInsteadOfSilentlyReturningZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SoundEffect.GetSampleSizeInBytes(TimeSpan.FromSeconds(1), 44100, (AudioChannels)0));
    }

    [Fact]
    public void Constructor_OffsetPlusCountOverflowsInt32_ThrowsRatherThanWrappingPastValidation()
    {
        // offset + count would overflow int32 and wrap negative if checked with a naive
        // "offset + count > buffer.Length" comparison, silently passing validation it should
        // fail. Must still throw ArgumentException, not reach the native call with a bad pointer.
        var buffer = new byte[16];

        Assert.Throws<ArgumentException>(() => new SoundEffect(buffer, int.MaxValue - 5, 20, 44100, AudioChannels.Mono, 0, 0));
    }

    [Fact]
    public void Constructor_OffsetBeyondBufferLength_Throws()
    {
        var buffer = new byte[16];

        Assert.Throws<ArgumentException>(() => new SoundEffect(buffer, 20, 0, 44100, AudioChannels.Mono, 0, 0));
    }

    [Fact]
    public void Constructor_NegativeLoopStart_Throws()
    {
        var buffer = new byte[16];

        Assert.Throws<ArgumentException>(() => new SoundEffect(buffer, 0, buffer.Length, 44100, AudioChannels.Mono, -1, 0));
    }

    [Fact]
    public void Constructor_NegativeLoopLength_Throws()
    {
        var buffer = new byte[16];

        Assert.Throws<ArgumentException>(() => new SoundEffect(buffer, 0, buffer.Length, 44100, AudioChannels.Mono, 0, -1));
    }

    [Fact]
    public void Constructor_UndefinedChannelsValue_Throws()
    {
        var buffer = new byte[16];

        Assert.Throws<ArgumentOutOfRangeException>(() => new SoundEffect(buffer, 0, buffer.Length, 44100, (AudioChannels)0, 0, 0));
    }
}
