using Xunit;
using XnaVisualizationData = Microsoft.Xna.Framework.Media.VisualizationData;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// The strict XNA facade exposes read-only collections while its internal CNA holder keeps the
/// writable arrays needed by MediaPlayer.
/// </summary>
public class VisualizationDataCompatTests
{
    [Fact]
    public void Constructor_FrequenciesHasCorrectLength()
    {
        var data = new XnaVisualizationData();

        Assert.Equal(CNA.Media.VisualizationData.Size, data.Frequencies.Count);
    }

    [Fact]
    public void Constructor_SamplesHasCorrectLength()
    {
        var data = new XnaVisualizationData();

        Assert.Equal(CNA.Media.VisualizationData.Size, data.Samples.Count);
    }

    [Fact]
    public void Constructor_FrequenciesAndSamplesStartAllZero()
    {
        var data = new XnaVisualizationData();

        Assert.All(data.Frequencies, value => Assert.Equal(0f, value));
        Assert.All(data.Samples, value => Assert.Equal(0f, value));
    }
}
