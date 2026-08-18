using CNA.Graphics;
using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// Pins the numeric value of every Phase 8 WP1 enum (see <c>plan.md</c>). These enums are cast
/// straight across the P/Invoke boundary to their <c>CNA_*</c> C counterparts rather than being
/// translated member-by-member, so a wrong or reordered member is not a compile error on either
/// side -- it is a silent, wrong value reaching native code. Asserting the numbers here is the
/// only thing that actually catches that, and it doubles as the record that these match real XNA
/// 4.0 (the numbers below were cross-checked against both real XNA and the C API headers named in
/// each enum's own doc comment).
/// </summary>
public class GraphicsEnumValueTests
{
    [Theory]
    [InlineData(SurfaceFormat.Color, 0)]
    [InlineData(SurfaceFormat.Bgr565, 1)]
    [InlineData(SurfaceFormat.Bgra5551, 2)]
    [InlineData(SurfaceFormat.Bgra4444, 3)]
    [InlineData(SurfaceFormat.Dxt1, 4)]
    [InlineData(SurfaceFormat.Dxt3, 5)]
    [InlineData(SurfaceFormat.Dxt5, 6)]
    [InlineData(SurfaceFormat.NormalizedByte2, 7)]
    [InlineData(SurfaceFormat.NormalizedByte4, 8)]
    [InlineData(SurfaceFormat.Rgba1010102, 9)]
    [InlineData(SurfaceFormat.Rg32, 10)]
    [InlineData(SurfaceFormat.Rgba64, 11)]
    [InlineData(SurfaceFormat.Alpha8, 12)]
    [InlineData(SurfaceFormat.Single, 13)]
    [InlineData(SurfaceFormat.Vector2, 14)]
    [InlineData(SurfaceFormat.Vector4, 15)]
    [InlineData(SurfaceFormat.HalfSingle, 16)]
    [InlineData(SurfaceFormat.HalfVector2, 17)]
    [InlineData(SurfaceFormat.HalfVector4, 18)]
    [InlineData(SurfaceFormat.HdrBlendable, 19)]
    public void SurfaceFormat_XnaMembers_MatchXnaValues(SurfaceFormat value, int expected) =>
        Assert.Equal(expected, (int)value);

    /// <summary>The <c>_EXT</c> half has no real-XNA counterpart to match -- it is pinned against
    /// the C API alone (<c>graphics.h:274-286</c>), and separately from the block above so the
    /// distinction stays visible.</summary>
    [Theory]
    [InlineData(SurfaceFormat.ColorBgraExt, 20)]
    [InlineData(SurfaceFormat.ColorSrgbExt, 21)]
    [InlineData(SurfaceFormat.Dxt5SrgbExt, 22)]
    [InlineData(SurfaceFormat.Bc7Ext, 23)]
    [InlineData(SurfaceFormat.Bc7SrgbExt, 24)]
    [InlineData(SurfaceFormat.ByteExt, 25)]
    [InlineData(SurfaceFormat.UShortExt, 26)]
    public void SurfaceFormat_CnaExtensions_MatchCApiValues(SurfaceFormat value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(SpriteSortMode.Deferred, 0)]
    [InlineData(SpriteSortMode.Immediate, 1)]
    [InlineData(SpriteSortMode.Texture, 2)]
    [InlineData(SpriteSortMode.BackToFront, 3)]
    [InlineData(SpriteSortMode.FrontToBack, 4)]
    public void SpriteSortMode_MatchesXnaValues(SpriteSortMode value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(DepthFormat.None, 0)]
    [InlineData(DepthFormat.Depth16, 1)]
    [InlineData(DepthFormat.Depth24, 2)]
    [InlineData(DepthFormat.Depth24Stencil8, 3)]
    public void DepthFormat_MatchesXnaValues(DepthFormat value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(CubeMapFace.PositiveX, 0)]
    [InlineData(CubeMapFace.NegativeX, 1)]
    [InlineData(CubeMapFace.PositiveY, 2)]
    [InlineData(CubeMapFace.NegativeY, 3)]
    [InlineData(CubeMapFace.PositiveZ, 4)]
    [InlineData(CubeMapFace.NegativeZ, 5)]
    public void CubeMapFace_MatchesXnaValues(CubeMapFace value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(RenderTargetUsage.DiscardContents, 0)]
    [InlineData(RenderTargetUsage.PreserveContents, 1)]
    [InlineData(RenderTargetUsage.PlatformContents, 2)]
    public void RenderTargetUsage_MatchesXnaValues(RenderTargetUsage value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(SetDataOptions.None, 0)]
    [InlineData(SetDataOptions.Discard, 1)]
    [InlineData(SetDataOptions.NoOverwrite, 2)]
    public void SetDataOptions_MatchesXnaValues(SetDataOptions value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(PresentInterval.Default, 0)]
    [InlineData(PresentInterval.One, 1)]
    [InlineData(PresentInterval.Two, 2)]
    [InlineData(PresentInterval.Immediate, 3)]
    public void PresentInterval_MatchesXnaValues(PresentInterval value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(GraphicsDeviceStatus.Normal, 0)]
    [InlineData(GraphicsDeviceStatus.Lost, 1)]
    [InlineData(GraphicsDeviceStatus.NotReset, 2)]
    public void GraphicsDeviceStatus_MatchesXnaValues(GraphicsDeviceStatus value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(TextureAddressMode.Wrap, 0)]
    [InlineData(TextureAddressMode.Clamp, 1)]
    [InlineData(TextureAddressMode.Mirror, 2)]
    public void TextureAddressMode_MatchesXnaValues(TextureAddressMode value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(TextureFilter.Linear, 0)]
    [InlineData(TextureFilter.Point, 1)]
    [InlineData(TextureFilter.Anisotropic, 2)]
    [InlineData(TextureFilter.LinearMipPoint, 3)]
    [InlineData(TextureFilter.PointMipLinear, 4)]
    [InlineData(TextureFilter.MinLinearMagPointMipLinear, 5)]
    [InlineData(TextureFilter.MinLinearMagPointMipPoint, 6)]
    [InlineData(TextureFilter.MinPointMagLinearMipLinear, 7)]
    [InlineData(TextureFilter.MinPointMagLinearMipPoint, 8)]
    public void TextureFilter_MatchesXnaValues(TextureFilter value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Theory]
    [InlineData(CNA.Input.GamePadDeadZone.None, 0)]
    [InlineData(CNA.Input.GamePadDeadZone.IndependentAxes, 1)]
    [InlineData(CNA.Input.GamePadDeadZone.Circular, 2)]
    public void GamePadDeadZone_MatchesXnaValues(CNA.Input.GamePadDeadZone value, int expected) =>
        Assert.Equal(expected, (int)value);

    /// <summary>Real XNA's <c>DisplayOrientation</c> skips 3 -- the members are independent bits,
    /// so <c>Portrait</c> is 4, not the 3 a dense sequence would give it. Pinned explicitly
    /// because that gap is exactly the kind of thing a well-meaning "tidy up the enum" edit
    /// silently closes.</summary>
    [Theory]
    [InlineData(DisplayOrientation.Default, 0)]
    [InlineData(DisplayOrientation.LandscapeLeft, 1)]
    [InlineData(DisplayOrientation.LandscapeRight, 2)]
    [InlineData(DisplayOrientation.Portrait, 4)]
    public void DisplayOrientation_MatchesXnaValues(DisplayOrientation value, int expected) =>
        Assert.Equal(expected, (int)value);

    [Fact]
    public void DisplayOrientation_IsFlags() =>
        Assert.True(typeof(DisplayOrientation).IsDefined(typeof(FlagsAttribute), inherit: false));
}
