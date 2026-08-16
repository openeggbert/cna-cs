using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// BasicEffect's constructor and every property setter are pure managed state (see
/// ../../src/CNA.Framework/Graphics/BasicEffect.cs) -- no native dependency, real and testable
/// today, confirmed against the real openeggbert/cna C++ engine's own implementation. Only
/// Apply() (via Effect.Apply) crosses into native code, so its internal computations
/// (FogVectorForTests/EyePositionWorldForTests) are exposed for direct testing instead.
/// </summary>
public class BasicEffectTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeHandleValue: 0);

    [Fact]
    public void Constructor_MatchesRealXnaDefaults()
    {
        var effect = new BasicEffect(CreateDummyDevice());

        Assert.Equal(Vector3.One, effect.DiffuseColor);
        Assert.Equal(Vector3.Zero, effect.EmissiveColor);
        Assert.Equal(Vector3.One, effect.SpecularColor);
        Assert.Equal(16f, effect.SpecularPower);
        Assert.Equal(Vector3.Zero, effect.AmbientLightColor);
        Assert.Equal(1f, effect.Alpha);
        Assert.False(effect.LightingEnabled);
        Assert.False(effect.PreferPerPixelLighting);
        Assert.False(effect.TextureEnabled);
        Assert.Null(effect.Texture);
        Assert.False(effect.FogEnabled);
        Assert.Equal(Vector3.Zero, effect.FogColor);
        Assert.Equal(0f, effect.FogStart);
        Assert.Equal(1f, effect.FogEnd);
        Assert.False(effect.VertexColorEnabled);
        Assert.Equal(Matrix.Identity, effect.World);
        Assert.Equal(Matrix.Identity, effect.View);
        Assert.Equal(Matrix.Identity, effect.Projection);
    }

    [Fact]
    public void Constructor_OnlyDirectionalLight0StartsEnabled()
    {
        var effect = new BasicEffect(CreateDummyDevice());

        Assert.True(effect.DirectionalLight0.Enabled);
        Assert.False(effect.DirectionalLight1.Enabled);
        Assert.False(effect.DirectionalLight2.Enabled);
    }

    [Fact]
    public void EnableDefaultLighting_SetsExactRealXnaValues()
    {
        var effect = new BasicEffect(CreateDummyDevice());

        effect.EnableDefaultLighting();

        Assert.True(effect.LightingEnabled);
        AssertVector3(new Vector3(0.05333332f, 0.09882354f, 0.1819608f), effect.AmbientLightColor);

        Assert.True(effect.DirectionalLight0.Enabled);
        AssertVector3(new Vector3(-0.5265408f, -0.5735765f, -0.6275069f), effect.DirectionalLight0.Direction);
        AssertVector3(new Vector3(1f, 0.9607844f, 0.8078432f), effect.DirectionalLight0.DiffuseColor);
        AssertVector3(new Vector3(1f, 0.9607844f, 0.8078432f), effect.DirectionalLight0.SpecularColor);

        Assert.True(effect.DirectionalLight1.Enabled);
        AssertVector3(new Vector3(0.7198464f, 0.3420201f, 0.6040227f), effect.DirectionalLight1.Direction);
        AssertVector3(new Vector3(0.9647059f, 0.7607844f, 0.4078432f), effect.DirectionalLight1.DiffuseColor);
        AssertVector3(Vector3.Zero, effect.DirectionalLight1.SpecularColor);

        Assert.True(effect.DirectionalLight2.Enabled);
        AssertVector3(new Vector3(0.4545195f, -0.7660444f, 0.4545195f), effect.DirectionalLight2.Direction);
        AssertVector3(new Vector3(0.3231373f, 0.3607844f, 0.3937255f), effect.DirectionalLight2.DiffuseColor);
        AssertVector3(new Vector3(0.3231373f, 0.3607844f, 0.3937255f), effect.DirectionalLight2.SpecularColor);

        Assert.Equal(Vector3.One, effect.SpecularColor);
        Assert.Equal(16f, effect.SpecularPower);
    }

    [Fact]
    public void FogVector_WhenFogDisabled_IsZero()
    {
        var effect = new BasicEffect(CreateDummyDevice()) { FogEnabled = false };

        Assert.Equal(Vector4.Zero, effect.FogVectorForTests);
    }

    [Fact]
    public void FogVector_WhenStartEqualsEnd_IsFullyFogged()
    {
        var effect = new BasicEffect(CreateDummyDevice())
        {
            FogEnabled = true,
            FogStart = 10f,
            FogEnd = 10f,
        };

        Assert.Equal(new Vector4(0f, 0f, 0f, 1f), effect.FogVectorForTests);
    }

    [Fact]
    public void FogVector_IdentityWorldView_MatchesHandComputedValue()
    {
        // World = View = Identity, FogStart=10, FogEnd=20 -> s = 1/(10-20) = -0.1.
        // fogWorldView = Identity, so M13=0, M23=0, M33=1, M43=0.
        // fogVector = (0*s, 0*s, 1*s, (0+FogStart)*s) = (0, 0, -0.1, -1.0).
        var effect = new BasicEffect(CreateDummyDevice())
        {
            FogEnabled = true,
            FogStart = 10f,
            FogEnd = 20f,
        };

        Vector4 fogVector = effect.FogVectorForTests;

        Assert.Equal(0f, fogVector.X, precision: 5);
        Assert.Equal(0f, fogVector.Y, precision: 5);
        Assert.Equal(-0.1f, fogVector.Z, precision: 5);
        Assert.Equal(-1.0f, fogVector.W, precision: 5);
    }

    [Fact]
    public void EyePositionWorld_WhenLightingDisabled_IsZero()
    {
        var effect = new BasicEffect(CreateDummyDevice())
        {
            LightingEnabled = false,
            View = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up),
        };

        Assert.Equal(Vector3.Zero, effect.EyePositionWorldForTests);
    }

    [Fact]
    public void EyePositionWorld_WhenLightingEnabled_RecoversCameraPosition()
    {
        var cameraPosition = new Vector3(3f, 4f, 10f);
        var effect = new BasicEffect(CreateDummyDevice())
        {
            LightingEnabled = true,
            View = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.Up),
        };

        Vector3 eyePosition = effect.EyePositionWorldForTests;

        Assert.Equal(cameraPosition.X, eyePosition.X, precision: 3);
        Assert.Equal(cameraPosition.Y, eyePosition.Y, precision: 3);
        Assert.Equal(cameraPosition.Z, eyePosition.Z, precision: 3);
    }

    private static void AssertVector3(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 5);
        Assert.Equal(expected.Y, actual.Y, precision: 5);
        Assert.Equal(expected.Z, actual.Z, precision: 5);
    }
}
