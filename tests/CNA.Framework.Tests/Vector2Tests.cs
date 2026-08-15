using CNA.Framework;
using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// Vector2 is a local, non-native value type (see ../../src/CNA.Framework/Vector2.cs), so these
/// tests run without any native CNA library present -- unlike almost everything else in this
/// repository right now. See plan.md "Hard dependency on openeggbert/cna".
/// </summary>
public class Vector2Tests
{
    [Fact]
    public void Constructor_SetsComponents()
    {
        var v = new Vector2(3f, 4f);

        Assert.Equal(3f, v.X);
        Assert.Equal(4f, v.Y);
    }

    [Fact]
    public void Length_ComputesEuclideanNorm()
    {
        var v = new Vector2(3f, 4f);

        Assert.Equal(5f, v.Length());
    }

    [Fact]
    public void Addition_AddsComponentwise()
    {
        var result = new Vector2(1f, 2f) + new Vector2(3f, 4f);

        Assert.Equal(new Vector2(4f, 6f), result);
    }

    [Fact]
    public void Normalize_ProducesUnitLength()
    {
        var v = new Vector2(3f, 4f);

        v.Normalize();

        Assert.Equal(1f, v.Length(), precision: 5);
    }

    [Fact]
    public void Equals_ComparesComponents()
    {
        Assert.Equal(new Vector2(1f, 2f), new Vector2(1f, 2f));
        Assert.NotEqual(new Vector2(1f, 2f), new Vector2(1f, 3f));
    }
}
