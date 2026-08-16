using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// GraphicsDevice.Clear/SetRenderTarget/SetVertexBuffer call native code unconditionally (nothing
/// to validate before the call), so they can't be tested here at all -- but DrawPrimitives/
/// DrawIndexedPrimitives validate their scalar arguments first, so those failure paths are
/// testable without a real cna-native, same reasoning as VertexBufferTests/IndexBufferTests.
/// </summary>
public class GraphicsDeviceTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeHandleValue: 0);

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public void DrawPrimitives_InvalidArguments_Throws(int startVertex, int primitiveCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDummyDevice().DrawPrimitives(PrimitiveType.TriangleList, startVertex, primitiveCount));
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, 1)]
    [InlineData(0, -1, 0, 0, 1)]
    [InlineData(0, 0, -1, 0, 1)]
    [InlineData(0, 0, 0, -1, 1)]
    [InlineData(0, 0, 0, 0, 0)]
    public void DrawIndexedPrimitives_InvalidArguments_Throws(
        int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateDummyDevice().DrawIndexedPrimitives(
            PrimitiveType.TriangleList, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount));
    }
}
