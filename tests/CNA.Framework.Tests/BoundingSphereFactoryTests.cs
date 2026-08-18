using CNA;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="BoundingSphere.CreateFromPoints"/>, <see cref="BoundingSphere.CreateFromFrustum"/> and
/// <see cref="BoundingSphere.Transform"/> are pure math, so they are fully testable here.
///
/// They were missing until a member-level diff against the engine's own XNA headers found them --
/// the earlier sweeps looked for *unbound native functions*, which structurally cannot see a
/// managed-only member.
///
/// The property worth testing is enclosure: every input point must end up inside the result. A
/// sphere that is merely plausible-looking passes an eyeball check and fails that one.
/// </summary>
public class BoundingSphereFactoryTests
{
    private const float Tolerance = 1e-4f;

    private static void AssertEncloses(BoundingSphere sphere, params Vector3[] points)
    {
        foreach (Vector3 point in points)
        {
            float distance = Vector3.Distance(sphere.Center, point);
            Assert.True(
                distance <= sphere.Radius + Tolerance,
                $"{point} is {distance} from the centre but the radius is only {sphere.Radius}.");
        }
    }

    [Fact]
    public void CreateFromPoints_SinglePoint_IsDegenerate()
    {
        var sphere = BoundingSphere.CreateFromPoints([new Vector3(3f, 4f, 5f)]);

        Assert.Equal(new Vector3(3f, 4f, 5f), sphere.Center);
        Assert.Equal(0f, sphere.Radius, Tolerance);
    }

    [Fact]
    public void CreateFromPoints_TwoPoints_SpansThemExactly()
    {
        var sphere = BoundingSphere.CreateFromPoints([new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f)]);

        Assert.Equal(Vector3.Zero.X, sphere.Center.X, Tolerance);
        Assert.Equal(1f, sphere.Radius, Tolerance);
    }

    /// <summary>
    /// The eight corners of a cube must all be enclosed, and the radius must at least reach the
    /// half-diagonal -- a per-axis-only implementation stops at the half-edge and leaves four
    /// corners outside.
    ///
    /// Deliberately a lower bound rather than an equality. Ritter's algorithm overestimates, and by
    /// how much depends on the order the points arrive in, so an exact radius is not a contract this
    /// can assert. The engine's own <c>BoundingSphereTests.cpp</c> asserts enclosure only, for the
    /// same reason -- an earlier draft of this test asserted the minimal radius and failed against a
    /// correct implementation.
    /// </summary>
    [Fact]
    public void CreateFromPoints_CubeCorners_EnclosesAllAndReachesAtLeastTheDiagonal()
    {
        Vector3[] corners =
        [
            new(-1f, -1f, -1f), new(1f, -1f, -1f), new(-1f, 1f, -1f), new(1f, 1f, -1f),
            new(-1f, -1f, 1f), new(1f, -1f, 1f), new(-1f, 1f, 1f), new(1f, 1f, 1f),
        ];

        var sphere = BoundingSphere.CreateFromPoints(corners);

        AssertEncloses(sphere, corners);
        Assert.True(
            sphere.Radius >= MathF.Sqrt(3f) - Tolerance,
            $"Radius {sphere.Radius} does not even reach the cube's half-diagonal {MathF.Sqrt(3f)}.");
    }

    /// <summary>The engine's own fixture, asserting what its own test asserts.</summary>
    [Fact]
    public void CreateFromPoints_EngineFixture_EnclosesAllPoints()
    {
        Vector3[] points = [new(-2f, 0f, 0f), new(2f, 0f, 0f), new(0f, 1f, 0f)];

        AssertEncloses(BoundingSphere.CreateFromPoints(points), points);
    }

    /// <summary>The growth step is the part that is easy to get wrong: a point outside the initial
    /// sphere must be taken in *without* dropping any point already inside. Re-centring on the mean
    /// instead of on the far side is the classic failure, and it shows up here.</summary>
    [Fact]
    public void CreateFromPoints_OutlierAfterCluster_StillEnclosesTheCluster()
    {
        Vector3[] points =
        [
            new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 1f, 0f), new(0f, 0f, 1f),
            new(1f, 1f, 1f), new(50f, 0f, 0f),
        ];

        var sphere = BoundingSphere.CreateFromPoints(points);

        AssertEncloses(sphere, points);
    }

    [Fact]
    public void CreateFromPoints_EmptySequence_Throws()
    {
        Assert.Throws<ArgumentException>(() => BoundingSphere.CreateFromPoints([]));
    }

    [Fact]
    public void CreateFromPoints_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BoundingSphere.CreateFromPoints(null!));
    }

    /// <summary>A frustum's sphere must contain its own corners. This is the check that catches a
    /// wrong corner order or a dropped corner, neither of which changes the radius much.</summary>
    [Fact]
    public void CreateFromFrustum_EnclosesEveryCorner()
    {
        Matrix view = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.6f, 1f, 50f);
        var frustum = new BoundingFrustum(view * projection);

        var sphere = BoundingSphere.CreateFromFrustum(frustum);

        AssertEncloses(sphere, frustum.GetCorners());
    }

    [Fact]
    public void Transform_Translation_MovesTheCentreAndKeepsTheRadius()
    {
        var sphere = new BoundingSphere(new Vector3(1f, 2f, 3f), 5f);

        BoundingSphere moved = sphere.Transform(Matrix.CreateTranslation(10f, 0f, -4f));

        Assert.Equal(new Vector3(11f, 2f, -1f).X, moved.Center.X, Tolerance);
        Assert.Equal(new Vector3(11f, 2f, -1f).Y, moved.Center.Y, Tolerance);
        Assert.Equal(new Vector3(11f, 2f, -1f).Z, moved.Center.Z, Tolerance);
        Assert.Equal(5f, moved.Radius, Tolerance);
    }

    [Fact]
    public void Transform_UniformScale_ScalesTheRadius()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 2f);

        Assert.Equal(6f, sphere.Transform(Matrix.CreateScale(3f)).Radius, Tolerance);
    }

    /// <summary>A non-uniform scale takes the <em>largest</em> axis, not an average and not the
    /// determinant: the sphere has to still enclose everything it did before, so the worst axis
    /// wins. An averaging implementation returns 2 here and leaves geometry outside.</summary>
    [Fact]
    public void Transform_NonUniformScale_UsesTheLargestAxis()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1f);

        BoundingSphere scaled = sphere.Transform(Matrix.CreateScale(1f, 3f, 2f));

        Assert.Equal(3f, scaled.Radius, Tolerance);
    }

    /// <summary>Rotation must not change the radius at all -- a rotation matrix has unit-length
    /// basis rows, so any implementation that accumulated them would drift.</summary>
    [Fact]
    public void Transform_Rotation_LeavesTheRadiusUnchanged()
    {
        var sphere = new BoundingSphere(new Vector3(1f, 0f, 0f), 4f);

        BoundingSphere rotated = sphere.Transform(Matrix.CreateRotationY(MathHelper.PiOver4));

        Assert.Equal(4f, rotated.Radius, Tolerance);
    }
}
