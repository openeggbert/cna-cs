using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// These tests exercise no native CNA library -- they only check that the
/// Microsoft.Xna.Framework-namespaced value types convert correctly to/from their CNA.Framework
/// counterparts, and that the two parallel Keys enums stay numerically identical (see
/// ../../src/CNA.XnaCompat/Microsoft/Xna/Framework/Input/Keys.cs). If either of these ever
/// breaks, every native-backed API silently starts marshalling the wrong values.
/// </summary>
public class CompatibilityTests
{
    [Fact]
    public void Vector2_ImplicitlyConvertsToAndFromFrameworkVector2()
    {
        var xna = new XnaVector2(1f, 2f);

        CNA.Framework.Vector2 framework = xna;
        XnaVector2 roundTripped = framework;

        Assert.Equal(1f, framework.X);
        Assert.Equal(2f, framework.Y);
        Assert.Equal(xna.X, roundTripped.X);
        Assert.Equal(xna.Y, roundTripped.Y);
    }

    [Fact]
    public void Color_ImplicitlyConvertsToAndFromFrameworkColor()
    {
        var xna = new XnaColor(10, 20, 30, 40);

        CNA.Framework.Color framework = xna;
        XnaColor roundTripped = framework;

        Assert.Equal(xna.R, framework.R);
        Assert.Equal(xna.G, framework.G);
        Assert.Equal(xna.B, framework.B);
        Assert.Equal(xna.A, framework.A);
        Assert.Equal(xna, roundTripped);
    }

    [Theory]
    [InlineData(XnaKeys.Escape, CNA.Framework.Input.Keys.Escape)]
    [InlineData(XnaKeys.Space, CNA.Framework.Input.Keys.Space)]
    [InlineData(XnaKeys.A, CNA.Framework.Input.Keys.A)]
    [InlineData(XnaKeys.D0, CNA.Framework.Input.Keys.D0)]
    public void Keys_NumericValuesMatchFrameworkKeys(XnaKeys xnaKey, CNA.Framework.Input.Keys frameworkKey)
    {
        Assert.Equal((int)frameworkKey, (int)xnaKey);
    }
}
