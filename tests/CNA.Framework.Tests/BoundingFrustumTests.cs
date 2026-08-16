using CNA;
using Xunit;

namespace CNA.Tests;

public class BoundingFrustumTests
{
    private static BoundingFrustum CreateStandardFrustum()
    {
        Matrix view = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1f, 1f, 100f);
        return new BoundingFrustum(view * projection);
    }

    [Fact]
    public void Contains_PointAtLookAtTarget_IsInside()
    {
        BoundingFrustum frustum = CreateStandardFrustum();

        Assert.True(frustum.Contains(Vector3.Zero));
    }

    [Fact]
    public void Contains_PointBehindCamera_IsOutside()
    {
        BoundingFrustum frustum = CreateStandardFrustum();

        Assert.False(frustum.Contains(new Vector3(0f, 0f, 50f)));
    }

    [Fact]
    public void Contains_PointFarBeyondFarPlane_IsOutside()
    {
        BoundingFrustum frustum = CreateStandardFrustum();

        Assert.False(frustum.Contains(new Vector3(0f, 0f, -1000f)));
    }

    [Fact]
    public void Contains_PointFarOffToTheSide_IsOutside()
    {
        BoundingFrustum frustum = CreateStandardFrustum();

        Assert.False(frustum.Contains(new Vector3(1000f, 0f, 0f)));
    }

    [Fact]
    public void Contains_BoundingSphereAroundOrigin_ReturnsIntersectsOrContains()
    {
        BoundingFrustum frustum = CreateStandardFrustum();
        var sphere = new BoundingSphere(Vector3.Zero, 1f);

        ContainmentType result = frustum.Contains(sphere);

        Assert.True(result is ContainmentType.Contains or ContainmentType.Intersects);
    }

    [Fact]
    public void Contains_BoundingSphereFarAway_IsDisjoint()
    {
        BoundingFrustum frustum = CreateStandardFrustum();
        var sphere = new BoundingSphere(new Vector3(0f, 0f, -1000f), 1f);

        Assert.Equal(ContainmentType.Disjoint, frustum.Contains(sphere));
    }

    [Fact]
    public void GetCorners_NearCornersAreCloserToCameraThanFarCorners()
    {
        BoundingFrustum frustum = CreateStandardFrustum();
        Vector3[] corners = frustum.GetCorners();
        var cameraPosition = new Vector3(0f, 0f, 10f);

        float nearAverageDistance = (Vector3.Distance(cameraPosition, corners[0])
            + Vector3.Distance(cameraPosition, corners[1])
            + Vector3.Distance(cameraPosition, corners[2])
            + Vector3.Distance(cameraPosition, corners[3])) / 4f;

        float farAverageDistance = (Vector3.Distance(cameraPosition, corners[4])
            + Vector3.Distance(cameraPosition, corners[5])
            + Vector3.Distance(cameraPosition, corners[6])
            + Vector3.Distance(cameraPosition, corners[7])) / 4f;

        Assert.True(nearAverageDistance < farAverageDistance);
    }
}
