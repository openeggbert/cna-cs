using Xunit;
using XnaVisualizationData = Microsoft.Xna.Framework.Media.VisualizationData;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// <see cref="XnaVisualizationData"/> has no explicit constructor of its own (a fully trivial
/// subclass -- see its own doc comment), so the implicit default constructor is public and
/// reachable here with no <c>InternalsVisibleTo</c> concern at all, unlike most other new compat
/// types this session.
/// </summary>
public class VisualizationDataCompatTests
{
    [Fact]
    public void Constructor_FrequenciesHasCorrectLength()
    {
        var data = new XnaVisualizationData();

        Assert.Equal(XnaVisualizationData.Size, data.Frequencies.Length);
    }

    [Fact]
    public void Constructor_SamplesHasCorrectLength()
    {
        var data = new XnaVisualizationData();

        Assert.Equal(XnaVisualizationData.Size, data.Samples.Length);
    }

    [Fact]
    public void Constructor_FrequenciesAndSamplesStartAllZero()
    {
        var data = new XnaVisualizationData();

        Assert.All(data.Frequencies, value => Assert.Equal(0f, value));
        Assert.All(data.Samples, value => Assert.Equal(0f, value));
    }
}
