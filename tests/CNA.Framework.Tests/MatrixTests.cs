using CNA;
using Xunit;

namespace CNA.Tests;

public class MatrixTests
{
    [Fact]
    public void Identity_TransformsVectorUnchanged()
    {
        var v = new Vector3(1f, 2f, 3f);

        Vector3 result = Vector3.Transform(v, Matrix.Identity);

        Assert.Equal(v.X, result.X, precision: 5);
        Assert.Equal(v.Y, result.Y, precision: 5);
        Assert.Equal(v.Z, result.Z, precision: 5);
    }

    [Fact]
    public void CreateTranslation_MovesPoint()
    {
        Matrix translation = Matrix.CreateTranslation(10f, 20f, 30f);

        Vector3 result = Vector3.Transform(Vector3.Zero, translation);

        Assert.Equal(new Vector3(10f, 20f, 30f), result);
    }

    [Fact]
    public void CreateScale_ScalesPoint()
    {
        Matrix scale = Matrix.CreateScale(2f, 3f, 4f);

        Vector3 result = Vector3.Transform(new Vector3(1f, 1f, 1f), scale);

        Assert.Equal(new Vector3(2f, 3f, 4f), result);
    }

    [Fact]
    public void CreateRotationZ_RotatesQuarterTurn()
    {
        Matrix rotation = Matrix.CreateRotationZ(MathHelper.PiOver2);

        Vector3 result = Vector3.Transform(Vector3.UnitX, rotation);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(1f, result.Y, precision: 4);
    }

    [Theory]
    [MemberData(nameof(InvertibleMatrices))]
    public void Invert_ProducesMultiplicativeIdentity(Matrix matrix)
    {
        Matrix inverse = Matrix.Invert(matrix);
        Matrix product = matrix * inverse;

        AssertApproximatelyIdentity(product);
    }

    public static IEnumerable<object[]> InvertibleMatrices()
    {
        yield return new object[] { Matrix.Identity };
        yield return new object[] { Matrix.CreateTranslation(5f, -3f, 12f) };
        yield return new object[] { Matrix.CreateScale(2f, 0.5f, 3f) };
        yield return new object[] { Matrix.CreateRotationX(0.7f) };
        yield return new object[] { Matrix.CreateRotationY(1.1f) };
        yield return new object[] { Matrix.CreateRotationZ(2.3f) };
        yield return new object[]
        {
            Matrix.CreateRotationY(0.4f) * Matrix.CreateScale(2f, 3f, 4f) * Matrix.CreateTranslation(1f, 2f, 3f),
        };
        yield return new object[]
        {
            Matrix.CreateLookAt(new Vector3(0f, 5f, 10f), Vector3.Zero, Vector3.Up),
        };
        yield return new object[]
        {
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 16f / 9f, 0.1f, 100f),
        };
    }

    private static void AssertApproximatelyIdentity(Matrix m)
    {
        Assert.Equal(1f, m.M11, precision: 3);
        Assert.Equal(1f, m.M22, precision: 3);
        Assert.Equal(1f, m.M33, precision: 3);
        Assert.Equal(1f, m.M44, precision: 3);

        Assert.Equal(0f, m.M12, precision: 3);
        Assert.Equal(0f, m.M13, precision: 3);
        Assert.Equal(0f, m.M21, precision: 3);
        Assert.Equal(0f, m.M23, precision: 3);
        Assert.Equal(0f, m.M31, precision: 3);
        Assert.Equal(0f, m.M32, precision: 3);
        Assert.Equal(0f, m.M41, precision: 3);
        Assert.Equal(0f, m.M42, precision: 3);
        Assert.Equal(0f, m.M43, precision: 3);
    }

    [Fact]
    public void CreateLookAt_PlacesTargetInFrontOfCamera()
    {
        var eye = new Vector3(0f, 0f, 10f);
        var target = Vector3.Zero;

        Matrix view = Matrix.CreateLookAt(eye, target, Vector3.Up);
        Vector3 transformedTarget = Vector3.Transform(target, view);

        // The target must land on the camera's forward axis (negative view-space Z), i.e. in
        // front of the camera, not behind it.
        Assert.True(transformedTarget.Z < 0f);
    }

    [Fact]
    public void CreatePerspective_MatchesEquivalentFieldOfViewMatrix()
    {
        const float fov = MathHelper.PiOver4;
        const float aspect = 16f / 9f;
        const float near = 0.1f;
        const float far = 100f;

        float height = 2f * near * MathF.Tan(fov * 0.5f);
        float width = height * aspect;

        Matrix fromWidthHeight = Matrix.CreatePerspective(width, height, near, far);
        Matrix fromFov = Matrix.CreatePerspectiveFieldOfView(fov, aspect, near, far);

        AssertApproximatelyEqual(fromFov, fromWidthHeight);
    }

    [Fact]
    public void CreatePerspectiveOffCenter_CenteredFrustum_MatchesCreatePerspective()
    {
        const float width = 4f;
        const float height = 3f;
        const float near = 0.1f;
        const float far = 100f;

        Matrix centered = Matrix.CreatePerspective(width, height, near, far);
        Matrix offCenter = Matrix.CreatePerspectiveOffCenter(-width / 2f, width / 2f, -height / 2f, height / 2f, near, far);

        AssertApproximatelyEqual(centered, offCenter);
    }

    public static IEnumerable<object[]> DecomposableMatrices()
    {
        yield return new object[] { new Vector3(1f, 1f, 1f), Quaternion.Identity, Vector3.Zero };
        yield return new object[] { new Vector3(2f, 3f, 4f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2), new Vector3(5f, -3f, 12f) };
        yield return new object[]
        {
            new Vector3(0.5f, 2f, 1.5f),
            Quaternion.CreateFromYawPitchRoll(0.4f, -0.9f, 1.7f),
            new Vector3(-1f, 2f, -3f),
        };
    }

    [Theory]
    [MemberData(nameof(DecomposableMatrices))]
    public void Decompose_RecoversScaleRotationAndTranslation(Vector3 scale, Quaternion rotation, Vector3 translation)
    {
        Matrix composed = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);

        bool succeeded = composed.Decompose(out Vector3 recoveredScale, out Quaternion recoveredRotation, out Vector3 recoveredTranslation);

        Assert.True(succeeded);
        Assert.Equal(scale.X, recoveredScale.X, precision: 4);
        Assert.Equal(scale.Y, recoveredScale.Y, precision: 4);
        Assert.Equal(scale.Z, recoveredScale.Z, precision: 4);
        Assert.Equal(translation.X, recoveredTranslation.X, precision: 4);
        Assert.Equal(translation.Y, recoveredTranslation.Y, precision: 4);
        Assert.Equal(translation.Z, recoveredTranslation.Z, precision: 4);

        // q and -q represent the same rotation.
        float dot = MathF.Abs(Quaternion.Dot(rotation, recoveredRotation));
        Assert.True(dot > 0.999f, $"|dot|={dot}");
    }

    [Fact]
    public void Decompose_ZeroScaleAxis_ReturnsFalse()
    {
        Matrix degenerate = Matrix.CreateScale(1f, 0f, 1f);

        bool succeeded = degenerate.Decompose(out _, out _, out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void CreateBillboard_FacesCameraAndStaysOrthonormal()
    {
        var objectPosition = new Vector3(1f, 2f, 3f);
        var cameraPosition = new Vector3(1f, 2f, 13f);

        Matrix billboard = Matrix.CreateBillboard(objectPosition, cameraPosition, Vector3.Up, null);

        Assert.Equal(objectPosition.X, billboard.Translation.X, precision: 4);
        Assert.Equal(objectPosition.Y, billboard.Translation.Y, precision: 4);
        Assert.Equal(objectPosition.Z, billboard.Translation.Z, precision: 4);

        // The billboard's Forward axis must point from the object toward the camera.
        Vector3 expectedForward = Vector3.Normalize(cameraPosition - objectPosition);
        Assert.Equal(expectedForward.X, billboard.Forward.X, precision: 4);
        Assert.Equal(expectedForward.Y, billboard.Forward.Y, precision: 4);
        Assert.Equal(expectedForward.Z, billboard.Forward.Z, precision: 4);

        AssertOrthonormalBasis(billboard);
    }

    private static void AssertOrthonormalBasis(Matrix m)
    {
        Vector3 right = new(m.M11, m.M12, m.M13);
        Vector3 up = new(m.M21, m.M22, m.M23);
        Vector3 backward = new(m.M31, m.M32, m.M33);

        Assert.Equal(1f, right.Length(), precision: 4);
        Assert.Equal(1f, up.Length(), precision: 4);
        Assert.Equal(1f, backward.Length(), precision: 4);
        Assert.Equal(0f, Vector3.Dot(right, up), precision: 4);
        Assert.Equal(0f, Vector3.Dot(up, backward), precision: 4);
        Assert.Equal(0f, Vector3.Dot(backward, right), precision: 4);
    }

    [Fact]
    public void CreateShadow_ProjectsPointOntoPlaneAlongLightDirection()
    {
        // Ground plane y=0, light straight down -- any point (x,h,z) should project to (x,0,z).
        var plane = new Plane(Vector3.Up, 0f);
        var lightDirection = new Vector3(0f, -1f, 0f);
        Matrix shadow = Matrix.CreateShadow(lightDirection, plane);

        var point = new Vector3(3f, 7f, -2f);
        Vector4 homogeneous = Vector4.Transform(new Vector4(point, 1f), shadow);
        var projected = new Vector3(homogeneous.X / homogeneous.W, homogeneous.Y / homogeneous.W, homogeneous.Z / homogeneous.W);

        Assert.Equal(3f, projected.X, precision: 4);
        Assert.Equal(0f, projected.Y, precision: 4);
        Assert.Equal(-2f, projected.Z, precision: 4);
    }

    [Fact]
    public void CreateReflection_MirrorsPointAcrossPlane()
    {
        // Plane y=0 (XZ-plane through the origin): (x, h, z) reflects to (x, -h, z).
        var plane = new Plane(Vector3.Up, 0f);
        Matrix reflection = Matrix.CreateReflection(plane);

        Vector3 result = Vector3.Transform(new Vector3(3f, 5f, -2f), reflection);

        Assert.Equal(3f, result.X, precision: 4);
        Assert.Equal(-5f, result.Y, precision: 4);
        Assert.Equal(-2f, result.Z, precision: 4);
    }

    [Fact]
    public void CreateReflection_PointOnPlane_IsUnchanged()
    {
        var plane = new Plane(Vector3.Up, 0f);
        Matrix reflection = Matrix.CreateReflection(plane);

        Vector3 result = Vector3.Transform(new Vector3(3f, 0f, -2f), reflection);

        Assert.Equal(3f, result.X, precision: 4);
        Assert.Equal(0f, result.Y, precision: 4);
        Assert.Equal(-2f, result.Z, precision: 4);
    }

    private static void AssertApproximatelyEqual(Matrix expected, Matrix actual)
    {
        Assert.Equal(expected.M11, actual.M11, precision: 4);
        Assert.Equal(expected.M12, actual.M12, precision: 4);
        Assert.Equal(expected.M13, actual.M13, precision: 4);
        Assert.Equal(expected.M14, actual.M14, precision: 4);
        Assert.Equal(expected.M21, actual.M21, precision: 4);
        Assert.Equal(expected.M22, actual.M22, precision: 4);
        Assert.Equal(expected.M23, actual.M23, precision: 4);
        Assert.Equal(expected.M24, actual.M24, precision: 4);
        Assert.Equal(expected.M31, actual.M31, precision: 4);
        Assert.Equal(expected.M32, actual.M32, precision: 4);
        Assert.Equal(expected.M33, actual.M33, precision: 4);
        Assert.Equal(expected.M34, actual.M34, precision: 4);
        Assert.Equal(expected.M41, actual.M41, precision: 4);
        Assert.Equal(expected.M42, actual.M42, precision: 4);
        Assert.Equal(expected.M43, actual.M43, precision: 4);
        Assert.Equal(expected.M44, actual.M44, precision: 4);
    }
}
