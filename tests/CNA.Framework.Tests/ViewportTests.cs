using CNA;
using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="Viewport"/>'s derived members are arithmetic over its six fields, so they are fully
/// testable here -- and the expectations below are the engine's own
/// <c>ViewportTests.cpp</c> values, not values re-derived from the formula being tested.
///
/// <see cref="Viewport.Project"/>/<see cref="Viewport.Unproject"/> are exactly the kind of code that
/// looks right and is wrong: a sign flip on Y, a perspective divide taken from the transformed point
/// instead of the source, or a rescale applied after the transform all produce plausible screen
/// coordinates.
/// </summary>
public class ViewportTests
{
    private const float Tolerance = 1e-3f;

    [Fact]
    public void AspectRatio_IsWidthOverHeight()
    {
        Assert.Equal(800f / 600f, new Viewport(0, 0, 800, 600).AspectRatio, Tolerance);
    }

    /// <summary>Zero rather than a division -- a caller feeding this straight into
    /// <c>Matrix.CreatePerspectiveFieldOfView</c> wants a finite answer, not an infinity.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(800, 0)]
    [InlineData(0, 600)]
    public void AspectRatio_IsZeroWhenEitherDimensionIsZero(int width, int height)
    {
        Assert.Equal(0f, new Viewport(0, 0, width, height).AspectRatio);
    }

    [Fact]
    public void Bounds_RoundTrips()
    {
        var viewport = new Viewport(3, 7, 640, 480);

        Assert.Equal(new Rectangle(3, 7, 640, 480), viewport.Bounds);
    }

    /// <summary>The setter must leave the depth range alone -- it is not part of the
    /// rectangle.</summary>
    [Fact]
    public void Bounds_Setter_PreservesDepthRange()
    {
        var viewport = new Viewport(0, 0, 100, 100, 0.25f, 0.75f)
        {
            Bounds = new Rectangle(10, 20, 320, 240),
        };

        Assert.Equal(new Rectangle(10, 20, 320, 240), viewport.Bounds);
        Assert.Equal(0.25f, viewport.MinDepth);
        Assert.Equal(0.75f, viewport.MaxDepth);
    }

    /// <summary>Equal to <see cref="Viewport.Bounds"/> in this engine, which is *not* what XNA's
    /// Xbox-era 5% overscan inset did. Pinned so a future "fix" toward the remembered behaviour
    /// fails loudly.</summary>
    [Fact]
    public void TitleSafeArea_EqualsBounds()
    {
        var viewport = new Viewport(0, 0, 800, 600);

        Assert.Equal(viewport.Bounds, viewport.TitleSafeArea);
    }

    /// <summary>The engine's own fixture values. The centre maps to the middle of the viewport, and
    /// the far corner to (Width, 0) -- the Y flip is what makes 1 become 0 rather than
    /// Height.</summary>
    [Theory]
    [InlineData(0f, 0f, 0f, 400f, 300f, 0f)]
    [InlineData(1f, 1f, 1f, 800f, 0f, 1f)]
    [InlineData(-1f, -1f, 0f, 0f, 600f, 0f)]
    public void Project_WithIdentityMatrices_MatchesTheEngine(
        float x, float y, float z, float expectedX, float expectedY, float expectedZ)
    {
        var viewport = new Viewport(0, 0, 800, 600);
        Matrix identity = Matrix.Identity;

        Vector3 result = viewport.Project(new Vector3(x, y, z), identity, identity, identity);

        Assert.Equal(expectedX, result.X, Tolerance);
        Assert.Equal(expectedY, result.Y, Tolerance);
        Assert.Equal(expectedZ, result.Z, Tolerance);
    }

    /// <summary>Project then Unproject must return the original point. This is the property that
    /// catches an inconsistent Y flip or depth rescale between the two -- each could be wrong on its
    /// own in a way that a one-directional test would not notice.</summary>
    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0.5f, -0.25f, 0.75f)]
    [InlineData(-0.9f, 0.9f, 0.1f)]
    public void ProjectThenUnproject_ReturnsTheOriginalPoint(float x, float y, float z)
    {
        var viewport = new Viewport(0, 0, 800, 600);
        Matrix identity = Matrix.Identity;
        var source = new Vector3(x, y, z);

        Vector3 roundTripped = viewport.Unproject(
            viewport.Project(source, identity, identity, identity), identity, identity, identity);

        Assert.Equal(source.X, roundTripped.X, Tolerance);
        Assert.Equal(source.Y, roundTripped.Y, Tolerance);
        Assert.Equal(source.Z, roundTripped.Z, Tolerance);
    }

    /// <summary>The viewport's own offset and depth range must participate, not just its size.
    /// A round trip through a viewport that is not at the origin catches an implementation that
    /// ignores X/Y or MinDepth/MaxDepth.</summary>
    [Fact]
    public void ProjectThenUnproject_HonoursOffsetAndDepthRange()
    {
        var viewport = new Viewport(50, 25, 320, 240, 0.2f, 0.8f);
        Matrix identity = Matrix.Identity;
        var source = new Vector3(0.25f, -0.5f, 0.5f);

        Vector3 projected = viewport.Project(source, identity, identity, identity);
        Vector3 roundTripped = viewport.Unproject(projected, identity, identity, identity);

        // The projected point must land inside the viewport's own rectangle and depth range --
        // an implementation that dropped the offset would still round-trip, but land at the origin.
        Assert.InRange(projected.X, 50f, 370f);
        Assert.InRange(projected.Y, 25f, 265f);
        Assert.InRange(projected.Z, 0.2f, 0.8f);

        Assert.Equal(source.X, roundTripped.X, Tolerance);
        Assert.Equal(source.Y, roundTripped.Y, Tolerance);
        Assert.Equal(source.Z, roundTripped.Z, Tolerance);
    }

    /// <summary>A perspective projection makes <c>w</c> differ from 1, which is the branch an
    /// orthographic-only test never reaches.</summary>
    [Fact]
    public void ProjectThenUnproject_UnderPerspective_ReturnsTheOriginalPoint()
    {
        var viewport = new Viewport(0, 0, 1280, 720);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, viewport.AspectRatio, 0.1f, 100f);
        Matrix view = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up);
        Matrix world = Matrix.Identity;
        var source = new Vector3(1.5f, -2f, 3f);

        Vector3 projected = viewport.Project(source, projection, view, world);
        Vector3 roundTripped = viewport.Unproject(projected, projection, view, world);

        Assert.Equal(source.X, roundTripped.X, 1e-2f);
        Assert.Equal(source.Y, roundTripped.Y, 1e-2f);
        Assert.Equal(source.Z, roundTripped.Z, 1e-2f);
    }

    /// <summary>Machine epsilon is 2^-24, about 5.96e-8. <see cref="float.Epsilon"/> is the smallest
    /// denormal and roughly 37 orders of magnitude smaller -- using it would make
    /// <c>WithinEpsilon</c> effectively an exact comparison, so the perspective divide would run for
    /// every orthographic projection too.</summary>
    [Fact]
    public void MachineEpsilon_IsTheAdditiveIdentityThreshold_NotFloatEpsilon()
    {
        Assert.Equal(1f, 1f + MathHelper.MachineEpsilonFloat);
        Assert.True(1f + (MathHelper.MachineEpsilonFloat * 2f) > 1f);
        Assert.True(MathHelper.MachineEpsilonFloat > float.Epsilon * 1e30f);
    }
}
