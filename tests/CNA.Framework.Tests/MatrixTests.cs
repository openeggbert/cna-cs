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
}
