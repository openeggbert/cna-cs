using CNA;
using Xunit;

namespace CNA.Tests;

public class MathHelperTests
{
    [Fact]
    public void Barycentric_ComputesWeightedSum()
    {
        // 0 + 0.3*(10-0) + 0.2*(20-0) = 3 + 4 = 7.
        Assert.Equal(7f, MathHelper.Barycentric(0f, 10f, 20f, 0.3f, 0.2f), precision: 5);
    }

    [Fact]
    public void Barycentric_AtOriginWeights_ReturnsFirstValue()
    {
        Assert.Equal(5f, MathHelper.Barycentric(5f, 10f, 20f, 0f, 0f), precision: 5);
    }

    [Theory]
    [InlineData(0f, 5f)]
    [InlineData(1f, 8f)]
    public void CatmullRom_AtEndpoints_ReturnsTheTwoInnerControlPoints(float amount, float expected)
    {
        // Catmull-Rom passes exactly through value2 at t=0 and value3 at t=1 by construction.
        float result = MathHelper.CatmullRom(value1: 0f, value2: 5f, value3: 8f, value4: 20f, amount);

        Assert.Equal(expected, result, precision: 4);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 1f)]
    public void Hermite_AtEndpoints_ReturnsTheEndpointValuesExactly(float amount, float expected)
    {
        float result = MathHelper.Hermite(value1: 0f, tangent1: 1f, value2: 1f, tangent2: 1f, amount);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Hermite_SymmetricTangents_MidpointIsExactlyHalfway()
    {
        float result = MathHelper.Hermite(value1: 0f, tangent1: 1f, value2: 1f, tangent2: 1f, amount: 0.5f);

        Assert.Equal(0.5f, result, precision: 5);
    }
}
