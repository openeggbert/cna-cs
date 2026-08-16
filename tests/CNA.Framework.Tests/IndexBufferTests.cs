using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>Same testability limitation as VertexBufferTests -- only constructor argument
/// validation is reachable without a real cna-native.</summary>
public class IndexBufferTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeHandleValue: 0);

    [Fact]
    public void Constructor_NullGraphicsDevice_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new IndexBuffer(null!, IndexElementSize.SixteenBits, 3, BufferUsage.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveIndexCount_Throws(int indexCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IndexBuffer(CreateDummyDevice(), IndexElementSize.SixteenBits, indexCount, BufferUsage.None));
    }
}
