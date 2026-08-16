using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// VertexDeclaration/VertexElement are pure data (see ../../src/CNA.Framework/Graphics/VertexDeclaration.cs)
/// -- no native dependency, real and testable today, same as the math value types.
/// </summary>
public class VertexDeclarationTests
{
    [Fact]
    public void Constructor_AutoComputesStrideFromMaxElementExtent()
    {
        var declaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));

        Assert.Equal(16, declaration.VertexStride);
    }

    [Fact]
    public void Constructor_ElementsNotInOffsetOrder_StillComputesCorrectStride()
    {
        // Stride must be the max(offset + size) across all elements, not the sum in declared
        // order and not dependent on declaration order.
        var declaration = new VertexDeclaration(
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));

        Assert.Equal(20, declaration.VertexStride);
    }

    [Fact]
    public void Constructor_ExplicitStride_UsesGivenValueVerbatim()
    {
        // An explicit stride can be larger than the tightest-fit computed stride (e.g. padding
        // for GPU alignment) -- must not be silently recomputed.
        var declaration = new VertexDeclaration(
            32,
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));

        Assert.Equal(32, declaration.VertexStride);
    }

    [Fact]
    public void GetVertexElements_ReturnsACopy_NotTheInternalArray()
    {
        var element = new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0);
        var declaration = new VertexDeclaration(element);

        VertexElement[] first = declaration.GetVertexElements();
        first[0] = new VertexElement(99, VertexElementFormat.Single, VertexElementUsage.Fog, 0);
        VertexElement[] second = declaration.GetVertexElements();

        Assert.Equal(element, second[0]);
    }

    [Theory]
    [InlineData(VertexElementFormat.Single, 4)]
    [InlineData(VertexElementFormat.Vector2, 8)]
    [InlineData(VertexElementFormat.Vector3, 12)]
    [InlineData(VertexElementFormat.Vector4, 16)]
    [InlineData(VertexElementFormat.Color, 4)]
    [InlineData(VertexElementFormat.Byte4, 4)]
    [InlineData(VertexElementFormat.Short2, 4)]
    [InlineData(VertexElementFormat.Short4, 8)]
    [InlineData(VertexElementFormat.NormalizedShort2, 4)]
    [InlineData(VertexElementFormat.NormalizedShort4, 8)]
    [InlineData(VertexElementFormat.HalfVector2, 4)]
    [InlineData(VertexElementFormat.HalfVector4, 8)]
    public void GetTypeSize_MatchesRealXnaByteSizes(VertexElementFormat format, int expectedSize)
    {
        Assert.Equal(expectedSize, VertexDeclaration.GetTypeSize(format));
    }

    [Fact]
    public void VertexPosition_HasExpectedStride()
    {
        Assert.Equal(12, VertexPosition.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void VertexPositionColor_HasExpectedStride()
    {
        Assert.Equal(16, VertexPositionColor.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void VertexPositionTexture_HasExpectedStride()
    {
        Assert.Equal(20, VertexPositionTexture.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void VertexPositionColorTexture_HasExpectedStride()
    {
        Assert.Equal(24, VertexPositionColorTexture.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void VertexPositionNormalTexture_HasExpectedStride()
    {
        Assert.Equal(32, VertexPositionNormalTexture.VertexDeclaration.VertexStride);
    }

    [Fact]
    public void VertexPositionColor_ImplementsIVertexTypeReturningItsOwnDeclaration()
    {
        var vertex = new VertexPositionColor(new Vector3(1f, 2f, 3f), new Color(255, 0, 0));

        IVertexType asInterface = vertex;

        Assert.Same(VertexPositionColor.VertexDeclaration, asInterface.VertexDeclaration);
    }

    [Fact]
    public void VertexPositionColor_Equals_ComparesBothFields()
    {
        var a = new VertexPositionColor(new Vector3(1f, 2f, 3f), new Color(255, 0, 0));
        var b = new VertexPositionColor(new Vector3(1f, 2f, 3f), new Color(255, 0, 0));
        var c = new VertexPositionColor(new Vector3(1f, 2f, 3f), new Color(0, 255, 0));

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
