using CNA;
using Xunit;

namespace CNA.Tests;

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

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        Vector2 result = Vector2.Lerp(new Vector2(0f, 0f), new Vector2(10f, 20f), 0.5f);

        Assert.Equal(new Vector2(5f, 10f), result);
    }

    [Fact]
    public void SmoothStep_AtEndpoints_ReturnsEndpointsExactly()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);

        Assert.Equal(a, Vector2.SmoothStep(a, b, 0f));
        Assert.Equal(b, Vector2.SmoothStep(a, b, 1f));
    }

    [Fact]
    public void Barycentric_MatchesPerComponentMathHelperFormula()
    {
        var v1 = new Vector2(0f, 0f);
        var v2 = new Vector2(10f, 0f);
        var v3 = new Vector2(0f, 20f);

        Vector2 result = Vector2.Barycentric(v1, v2, v3, 0.3f, 0.2f);

        Assert.Equal(3f, result.X, precision: 4);
        Assert.Equal(4f, result.Y, precision: 4);
    }

    [Fact]
    public void CatmullRom_AtEndpoints_ReturnsInnerControlPoints()
    {
        var v1 = new Vector2(0f, 0f);
        var v2 = new Vector2(5f, 1f);
        var v3 = new Vector2(8f, 2f);
        var v4 = new Vector2(20f, 3f);

        Assert.Equal(v2, Vector2.CatmullRom(v1, v2, v3, v4, 0f));
        Assert.Equal(v3, Vector2.CatmullRom(v1, v2, v3, v4, 1f));
    }

    [Fact]
    public void Hermite_AtEndpoints_ReturnsEndpointValuesExactly()
    {
        var v1 = new Vector2(0f, 0f);
        var t1 = new Vector2(1f, 1f);
        var v2 = new Vector2(1f, 1f);
        var t2 = new Vector2(1f, 1f);

        Assert.Equal(v1, Vector2.Hermite(v1, t1, v2, t2, 0f));
        Assert.Equal(v2, Vector2.Hermite(v1, t1, v2, t2, 1f));
    }
}
