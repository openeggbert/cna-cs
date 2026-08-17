using Xunit;
using XnaModelMeshPart = Microsoft.Xna.Framework.Graphics.ModelMeshPart;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// <see cref="XnaModelMeshPart"/> is reachable from this test project the same way
/// <see cref="Microsoft.Xna.Framework.Graphics.ModelBone"/> is: neither constructor needs a real
/// <c>GraphicsDevice</c>. <c>Model</c>/<c>ModelMesh</c>/<c>ModelMeshPartCollection</c>/compat
/// <c>VertexBuffer</c>/<c>IndexBuffer</c>/<c>BasicEffect</c> (needed to exercise <c>Effect</c>/
/// <c>VertexBuffer</c>/<c>IndexBuffer</c> with real, non-null values) all do, so only this type's
/// own construction/field-storage/<c>SetXxx</c> surface is exercisable here.
/// </summary>
public class ModelMeshPartCompatTests
{
    [Fact]
    public void ParameterlessConstructor_EveryFieldStartsAtDefault()
    {
        var part = new XnaModelMeshPart();

        Assert.Equal(0, part.NumVertices);
        Assert.Equal(0, part.PrimitiveCount);
        Assert.Equal(0, part.StartIndex);
        Assert.Equal(0, part.VertexOffset);
        Assert.Null(part.VertexBuffer);
        Assert.Null(part.IndexBuffer);
        Assert.Null(part.Effect);
        Assert.Null(part.Tag);
    }

    [Fact]
    public void FullConstructor_SetsEveryField()
    {
        var part = new XnaModelMeshPart(null, null, numVertices: 4, primitiveCount: 2, startIndex: 6, vertexOffset: 1);

        Assert.Equal(4, part.NumVertices);
        Assert.Equal(2, part.PrimitiveCount);
        Assert.Equal(6, part.StartIndex);
        Assert.Equal(1, part.VertexOffset);
        Assert.Null(part.VertexBuffer);
        Assert.Null(part.IndexBuffer);
    }

    [Fact]
    public void SetVertexOffset_UpdatesValue()
    {
        var part = new XnaModelMeshPart();

        part.SetVertexOffset(5);

        Assert.Equal(5, part.VertexOffset);
    }

    [Fact]
    public void SetNumVertices_UpdatesValue()
    {
        var part = new XnaModelMeshPart();

        part.SetNumVertices(7);

        Assert.Equal(7, part.NumVertices);
    }

    [Fact]
    public void SetStartIndex_UpdatesValue()
    {
        var part = new XnaModelMeshPart();

        part.SetStartIndex(3);

        Assert.Equal(3, part.StartIndex);
    }

    [Fact]
    public void SetPrimitiveCount_UpdatesValue()
    {
        var part = new XnaModelMeshPart();

        part.SetPrimitiveCount(9);

        Assert.Equal(9, part.PrimitiveCount);
    }

    [Fact]
    public void SetVertexBuffer_NullValue_StaysNull()
    {
        var part = new XnaModelMeshPart();

        part.SetVertexBuffer(null);

        Assert.Null(part.VertexBuffer);
    }

    [Fact]
    public void SetIndexBuffer_NullValue_StaysNull()
    {
        var part = new XnaModelMeshPart();

        part.SetIndexBuffer(null);

        Assert.Null(part.IndexBuffer);
    }

    [Fact]
    public void Tag_IsSettable()
    {
        var part = new XnaModelMeshPart();
        var tag = new object();

        part.Tag = tag;

        Assert.Same(tag, part.Tag);
    }

    [Fact]
    public void Effect_SetToNull_StaysNull()
    {
        var part = new XnaModelMeshPart();

        part.Effect = null;

        Assert.Null(part.Effect);
    }
}
