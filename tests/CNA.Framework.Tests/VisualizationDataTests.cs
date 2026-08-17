using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="VisualizationData"/> is pure managed data -- no native dependency at all, unlike
/// <see cref="MediaPlayer.GetVisualizationData"/> itself (which populates it via a real native
/// call -- see <c>MediaPlayerTests</c>'s own coverage of that method's validation-only-testable
/// surface).
/// </summary>
public class VisualizationDataTests
{
    [Fact]
    public void Constructor_FrequenciesHasCorrectLength()
    {
        var data = new VisualizationData();

        Assert.Equal(VisualizationData.Size, data.Frequencies.Length);
    }

    [Fact]
    public void Constructor_SamplesHasCorrectLength()
    {
        var data = new VisualizationData();

        Assert.Equal(VisualizationData.Size, data.Samples.Length);
    }

    [Fact]
    public void Constructor_FrequenciesStartsAllZero()
    {
        var data = new VisualizationData();

        Assert.All(data.Frequencies, value => Assert.Equal(0f, value));
    }

    [Fact]
    public void Constructor_SamplesStartsAllZero()
    {
        var data = new VisualizationData();

        Assert.All(data.Samples, value => Assert.Equal(0f, value));
    }

    [Fact]
    public void Size_Is256()
    {
        Assert.Equal(256, VisualizationData.Size);
    }
}
