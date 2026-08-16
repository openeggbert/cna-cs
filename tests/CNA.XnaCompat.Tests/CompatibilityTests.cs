using Xunit;
using XnaAudioChannels = Microsoft.Xna.Framework.Audio.AudioChannels;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaSoundState = Microsoft.Xna.Framework.Audio.SoundState;
using XnaSpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// These tests exercise no native CNA library -- they only check that the
/// Microsoft.Xna.Framework-namespaced value types convert correctly to/from their CNA
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

        CNA.Vector2 framework = xna;
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

        CNA.Color framework = xna;
        XnaColor roundTripped = framework;

        Assert.Equal(xna.R, framework.R);
        Assert.Equal(xna.G, framework.G);
        Assert.Equal(xna.B, framework.B);
        Assert.Equal(xna.A, framework.A);
        Assert.Equal(xna, roundTripped);
    }

    [Theory]
    [InlineData(XnaKeys.Escape, CNA.Input.Keys.Escape)]
    [InlineData(XnaKeys.Space, CNA.Input.Keys.Space)]
    [InlineData(XnaKeys.A, CNA.Input.Keys.A)]
    [InlineData(XnaKeys.D0, CNA.Input.Keys.D0)]
    [InlineData(XnaKeys.F12, CNA.Input.Keys.F12)]
    [InlineData(XnaKeys.LeftShift, CNA.Input.Keys.LeftShift)]
    [InlineData(XnaKeys.OemTilde, CNA.Input.Keys.OemTilde)]
    [InlineData(XnaKeys.Kana, CNA.Input.Keys.Kana)]
    [InlineData(XnaKeys.ImeConvert, CNA.Input.Keys.ImeConvert)]
    [InlineData(XnaKeys.ChatPadGreen, CNA.Input.Keys.ChatPadGreen)]
    [InlineData(XnaKeys.ProcessKey, CNA.Input.Keys.ProcessKey)]
    [InlineData(XnaKeys.Pa1, CNA.Input.Keys.Pa1)]
    [InlineData(XnaKeys.OemClear, CNA.Input.Keys.OemClear)]
    public void Keys_NumericValuesMatchFrameworkKeys(XnaKeys xnaKey, CNA.Input.Keys frameworkKey)
    {
        Assert.Equal((int)frameworkKey, (int)xnaKey);
    }

    [Fact]
    public void Color_Transparent_IsWhiteWithZeroAlpha()
    {
        var color = XnaColor.Transparent;

        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
        Assert.Equal(0, color.A);
    }

    [Fact]
    public void Vector3_ImplicitlyConvertsAndDelegatesMath()
    {
        var xna = new Microsoft.Xna.Framework.Vector3(3f, 4f, 0f);

        CNA.Vector3 framework = xna;

        Assert.Equal(5f, xna.Length());
        Assert.Equal(5f, framework.Length());
    }

    [Fact]
    public void Matrix_Identity_TransformsVectorUnchanged()
    {
        var v = new Microsoft.Xna.Framework.Vector3(1f, 2f, 3f);

        Microsoft.Xna.Framework.Vector3 result =
            Microsoft.Xna.Framework.Vector3.Transform(v, Microsoft.Xna.Framework.Matrix.Identity);

        Assert.Equal(v.X, result.X, precision: 5);
        Assert.Equal(v.Y, result.Y, precision: 5);
        Assert.Equal(v.Z, result.Z, precision: 5);
    }

    [Fact]
    public void Matrix_Invert_RoundTripsThroughFrameworkDelegation()
    {
        Microsoft.Xna.Framework.Matrix matrix = Microsoft.Xna.Framework.Matrix.CreateTranslation(1f, 2f, 3f)
            * Microsoft.Xna.Framework.Matrix.CreateRotationY(0.5f);

        Microsoft.Xna.Framework.Matrix inverse = Microsoft.Xna.Framework.Matrix.Invert(matrix);
        Microsoft.Xna.Framework.Matrix product = matrix * inverse;

        Assert.Equal(1f, product.M11, precision: 3);
        Assert.Equal(1f, product.M22, precision: 3);
        Assert.Equal(1f, product.M33, precision: 3);
        Assert.Equal(1f, product.M44, precision: 3);
    }

    [Fact]
    public void Rectangle_ContainsAndIntersects_DelegateCorrectly()
    {
        var outer = new Microsoft.Xna.Framework.Rectangle(0, 0, 100, 100);
        var inner = new Microsoft.Xna.Framework.Rectangle(10, 10, 10, 10);
        var overlapping = new Microsoft.Xna.Framework.Rectangle(90, 90, 20, 20);

        Assert.True(outer.Contains(inner));
        Assert.True(outer.Intersects(overlapping));
    }

    [Theory]
    [InlineData(XnaSpriteEffects.None, CNA.Graphics.SpriteEffects.None)]
    [InlineData(XnaSpriteEffects.FlipHorizontally, CNA.Graphics.SpriteEffects.FlipHorizontally)]
    [InlineData(XnaSpriteEffects.FlipVertically, CNA.Graphics.SpriteEffects.FlipVertically)]
    public void SpriteEffects_NumericValuesMatchFrameworkSpriteEffects(
        XnaSpriteEffects xnaEffects, CNA.Graphics.SpriteEffects frameworkEffects)
    {
        Assert.Equal((int)frameworkEffects, (int)xnaEffects);
    }

    [Theory]
    [InlineData(XnaAudioChannels.Mono, CNA.Audio.AudioChannels.Mono)]
    [InlineData(XnaAudioChannels.Stereo, CNA.Audio.AudioChannels.Stereo)]
    public void AudioChannels_NumericValuesMatchFrameworkAudioChannels(
        XnaAudioChannels xnaChannels, CNA.Audio.AudioChannels frameworkChannels)
    {
        Assert.Equal((int)frameworkChannels, (int)xnaChannels);
    }

    [Theory]
    [InlineData(XnaSoundState.Playing, CNA.Audio.SoundState.Playing)]
    [InlineData(XnaSoundState.Paused, CNA.Audio.SoundState.Paused)]
    [InlineData(XnaSoundState.Stopped, CNA.Audio.SoundState.Stopped)]
    public void SoundState_NumericValuesMatchFrameworkSoundState(
        XnaSoundState xnaState, CNA.Audio.SoundState frameworkState)
    {
        Assert.Equal((int)frameworkState, (int)xnaState);
    }

    [Fact]
    public void SoundEffect_GetSampleDuration_MatchesFrameworkGetSampleDuration()
    {
        TimeSpan xna = Microsoft.Xna.Framework.Audio.SoundEffect.GetSampleDuration(88200, 44100, XnaAudioChannels.Mono);
        TimeSpan framework = CNA.Audio.SoundEffect.GetSampleDuration(88200, 44100, CNA.Audio.AudioChannels.Mono);

        Assert.Equal(framework, xna);
    }

    [Fact]
    public void VertexElement_ImplicitlyConvertsToAndFromFrameworkVertexElement()
    {
        var xna = new Microsoft.Xna.Framework.Graphics.VertexElement(
            12,
            Microsoft.Xna.Framework.Graphics.VertexElementFormat.Color,
            Microsoft.Xna.Framework.Graphics.VertexElementUsage.Color,
            0);

        CNA.Graphics.VertexElement framework = xna;
        Microsoft.Xna.Framework.Graphics.VertexElement roundTripped = framework;

        Assert.Equal(12, framework.Offset);
        Assert.Equal(CNA.Graphics.VertexElementFormat.Color, framework.VertexElementFormat);
        Assert.Equal(CNA.Graphics.VertexElementUsage.Color, framework.VertexElementUsage);
        Assert.Equal(xna, roundTripped);
    }

    /// <summary>Exhaustive (every defined member, not a hand-picked subset) parity check --
    /// unlike the small spot-checks for Keys/Buttons, this enumerates both enums via reflection
    /// so a future member added to one side without the other fails automatically, rather than
    /// relying on someone remembering to add a matching InlineData row.</summary>
    [Fact]
    public void VertexElementFormat_AllMembersMatchFrameworkVertexElementFormat() =>
        AssertEnumsNumericallyIdentical<CNA.Graphics.VertexElementFormat, Microsoft.Xna.Framework.Graphics.VertexElementFormat>();

    [Fact]
    public void VertexElementUsage_AllMembersMatchFrameworkVertexElementUsage() =>
        AssertEnumsNumericallyIdentical<CNA.Graphics.VertexElementUsage, Microsoft.Xna.Framework.Graphics.VertexElementUsage>();

    private static void AssertEnumsNumericallyIdentical<TFramework, TXna>()
        where TFramework : struct, Enum
        where TXna : struct, Enum
    {
        string[] frameworkNames = Enum.GetNames<TFramework>();
        string[] xnaNames = Enum.GetNames<TXna>();
        Assert.Equal(frameworkNames.OrderBy(n => n, StringComparer.Ordinal), xnaNames.OrderBy(n => n, StringComparer.Ordinal));

        foreach (string name in frameworkNames)
        {
            var frameworkValue = (int)(object)Enum.Parse<TFramework>(name);
            var xnaValue = (int)(object)Enum.Parse<TXna>(name);
            Assert.True(frameworkValue == xnaValue, $"{name}: framework={frameworkValue}, xna={xnaValue}");
        }
    }

    [Fact]
    public void VertexPositionColor_StrideMatchesFrameworkVertexPositionColor()
    {
        Assert.Equal(
            CNA.Graphics.VertexPositionColor.VertexDeclaration.VertexStride,
            Microsoft.Xna.Framework.Graphics.VertexPositionColor.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void BoundingFrustum_GetCorners_ReturnsCompatVector3Array()
    {
        Microsoft.Xna.Framework.Matrix view = Microsoft.Xna.Framework.Matrix.CreateLookAt(
            new Microsoft.Xna.Framework.Vector3(0f, 0f, 10f), Microsoft.Xna.Framework.Vector3.Zero, Microsoft.Xna.Framework.Vector3.Up);
        Microsoft.Xna.Framework.Matrix projection = Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(
            Microsoft.Xna.Framework.MathHelper.PiOver4, 1f, 1f, 100f);

        var frustum = new Microsoft.Xna.Framework.BoundingFrustum(view * projection);
        Microsoft.Xna.Framework.Vector3[] corners = frustum.GetCorners();

        Assert.Equal(8, corners.Length);
        Assert.True(frustum.Contains(Microsoft.Xna.Framework.Vector3.Zero));
    }
}
