using CNA;
using Xunit;

namespace CNA.Tests;

public class QuaternionTests
{
    public static readonly TheoryData<Quaternion> RotationSamples = new()
    {
        Quaternion.Identity,
        Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver4),
        Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2),
        Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.Pi * 0.75f),
        Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1f, 1f, 1f)), 1.234f),
        Quaternion.CreateFromYawPitchRoll(0.4f, -0.9f, 1.7f),
    };

    [Theory]
    [MemberData(nameof(RotationSamples))]
    public void CreateFromRotationMatrix_RoundTripsThroughCreateFromQuaternion(Quaternion original)
    {
        Matrix matrix = Matrix.CreateFromQuaternion(original);

        Quaternion recovered = Quaternion.CreateFromRotationMatrix(matrix);

        // q and -q represent the same rotation, so compare via |dot| rather than direct equality.
        float dot = MathF.Abs(Quaternion.Dot(original, recovered));
        Assert.True(dot > 0.9999f, $"|dot|={dot} for {original} -> {recovered}");
    }

    [Theory]
    [MemberData(nameof(RotationSamples))]
    public void CreateFromRotationMatrix_TransformsVectorsIdenticallyToTheSourceMatrix(Quaternion original)
    {
        Matrix matrix = Matrix.CreateFromQuaternion(original);
        Quaternion recovered = Quaternion.CreateFromRotationMatrix(matrix);

        Vector3 probe = new(1.5f, -2.25f, 0.75f);
        Vector3 viaMatrix = Vector3.Transform(probe, matrix);
        Vector3 viaRecoveredQuaternion = Vector3.Transform(probe, recovered);

        Assert.Equal(viaMatrix.X, viaRecoveredQuaternion.X, precision: 4);
        Assert.Equal(viaMatrix.Y, viaRecoveredQuaternion.Y, precision: 4);
        Assert.Equal(viaMatrix.Z, viaRecoveredQuaternion.Z, precision: 4);
    }

    [Fact]
    public void Slerp_AtZero_ReturnsFirstQuaternion()
    {
        Quaternion a = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);
        Quaternion b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.9f);

        Quaternion result = Quaternion.Slerp(a, b, 0f);

        Assert.Equal(a.X, result.X, precision: 5);
        Assert.Equal(a.Y, result.Y, precision: 5);
        Assert.Equal(a.Z, result.Z, precision: 5);
        Assert.Equal(a.W, result.W, precision: 5);
    }

    [Fact]
    public void Slerp_AtOne_ReturnsSecondQuaternion()
    {
        Quaternion a = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);
        Quaternion b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.9f);

        Quaternion result = Quaternion.Slerp(a, b, 1f);

        Assert.Equal(b.X, result.X, precision: 5);
        Assert.Equal(b.Y, result.Y, precision: 5);
        Assert.Equal(b.Z, result.Z, precision: 5);
        Assert.Equal(b.W, result.W, precision: 5);
    }

    [Fact]
    public void Slerp_Halfway_StaysUnitLengthAndMatchesHalfAngle()
    {
        Quaternion a = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0f);
        Quaternion b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2);

        Quaternion result = Quaternion.Slerp(a, b, 0.5f);
        Quaternion expected = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver4);

        Assert.Equal(1f, result.Length(), precision: 5);
        Assert.Equal(expected.X, result.X, precision: 4);
        Assert.Equal(expected.Y, result.Y, precision: 4);
        Assert.Equal(expected.Z, result.Z, precision: 4);
        Assert.Equal(expected.W, result.W, precision: 4);
    }

    [Fact]
    public void Slerp_TakesShortestPath_WhenQuaternionsAreMoreThanNinetyDegreesApart()
    {
        // -q represents the same rotation as q but has a negative dot product with a nearby
        // quaternion; Slerp must still interpolate along the short way around, not the long way.
        Quaternion a = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.1f);
        Quaternion negatedB = -Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.2f);

        Quaternion result = Quaternion.Slerp(a, negatedB, 0.5f);

        Vector3 probe = Vector3.UnitX;
        Vector3 rotatedProbe = Vector3.Transform(probe, result);
        Vector3 expectedProbe = Vector3.Transform(probe, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.15f));

        Assert.Equal(expectedProbe.X, rotatedProbe.X, precision: 3);
        Assert.Equal(expectedProbe.Y, rotatedProbe.Y, precision: 3);
    }
}
