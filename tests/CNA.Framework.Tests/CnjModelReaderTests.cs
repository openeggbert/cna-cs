using CNA.Content;
using CNA.Content.Cnj;
using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="CnjModelReader"/> is pure C#/BCL logic with zero native dependency -- like
/// <c>XnbModelReaderTests</c>, this is fully, end-to-end testable, including against a real fixture
/// reproducing the real openeggbert/cna C++ engine's own gtest fixture byte-for-byte (see
/// <c>assets/cnj/README.md</c> for provenance). <see cref="CnjModelBuilder"/> (the step that
/// actually constructs native-backed <c>VertexBuffer</c>/<c>IndexBuffer</c> instances) is
/// deliberately not exercised here -- that part needs a real <c>cna-native</c>, same as every other
/// native-backed type in this project.
/// </summary>
public class CnjModelReaderTests
{
    private static readonly string AssetsDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "cnj");

    [Fact]
    public void Read_RealQuadFixture_ProducesExpectedModelData()
    {
        string json = File.ReadAllText(Path.Combine(AssetsDirectory, "quad.cnj"));

        CnjModelData data = CnjModelReader.Read(json, "quad", AssetsDirectory);

        CnjMeshData mesh = Assert.Single(data.Meshes);
        Assert.Equal("Quad", mesh.Name);

        Assert.Equal(VertexPositionNormalTexture.VertexDeclaration.VertexStride, mesh.VertexBuffer.Declaration.VertexStride);
        Assert.Equal(4, mesh.VertexBuffer.VertexCount);
        Assert.Equal(128, mesh.VertexBuffer.Data.Length);

        Assert.True(mesh.IndexBuffer.SixteenBits);
        Assert.Equal(12, mesh.IndexBuffer.Data.Length);
        Assert.Equal(2, mesh.PrimitiveCount);

        Assert.Null(mesh.Effect.TextureReference);
        Assert.False(mesh.Effect.VertexColorEnabled);
    }

    [Fact]
    public void Read_RealQuadFixture_VertexBytesMatchExpectedLayout()
    {
        string json = File.ReadAllText(Path.Combine(AssetsDirectory, "quad.cnj"));
        CnjModelData data = CnjModelReader.Read(json, "quad", AssetsDirectory);
        byte[] bytes = data.Meshes[0].VertexBuffer.Data;

        (float x, float y, float z)[] expectedPositions =
        [
            (-0.5f, 0.5f, 0.0f),
            (-0.5f, -0.5f, 0.0f),
            (0.5f, -0.5f, 0.0f),
            (0.5f, 0.5f, 0.0f),
        ];

        for (int i = 0; i < expectedPositions.Length; i++)
        {
            int offset = i * 32;
            float x = BitConverter.ToSingle(bytes, offset);
            float y = BitConverter.ToSingle(bytes, offset + 4);
            float z = BitConverter.ToSingle(bytes, offset + 8);
            Assert.Equal(expectedPositions[i].x, x);
            Assert.Equal(expectedPositions[i].y, y);
            Assert.Equal(expectedPositions[i].z, z);
        }
    }

    [Fact]
    public void Read_RealQuadFixture_IndexBytesMatchExpectedTriangles()
    {
        string json = File.ReadAllText(Path.Combine(AssetsDirectory, "quad.cnj"));
        CnjModelData data = CnjModelReader.Read(json, "quad", AssetsDirectory);
        byte[] bytes = data.Meshes[0].IndexBuffer.Data;

        ushort[] indices = new ushort[6];
        Buffer.BlockCopy(bytes, 0, indices, 0, 12);
        Assert.Equal([(ushort)0, (ushort)1, (ushort)2, (ushort)0, (ushort)2, (ushort)3], indices);
    }

    [Fact]
    public void Read_MismatchedType_ThrowsContentLoadException()
    {
        string json = File.ReadAllText(Path.Combine(AssetsDirectory, "mismatched_type.cnj"));

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "mismatched_type", AssetsDirectory));
    }

    [Theory]
    [InlineData("""{"type":"Model"}""")] // missing cnjVersion entirely
    [InlineData("""{"cnjVersion":1.5,"type":"Model"}""")] // non-integer version, not truncated
    [InlineData("""{"cnjVersion":"1","type":"Model"}""")] // wrong JSON kind (string, not number)
    public void Read_InvalidCnjVersion_ThrowsContentLoadException(string json)
    {
        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_UnsupportedCnjVersion_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":2,"type":"Model"}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_SourceFileField_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":1,"type":"Model","sourceFile":"model.blend"}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_SkeletonField_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":1,"type":"Model","skeleton":{}}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_MultiEntryBonesArray_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":1,"type":"Model","bones":[{"name":"A"},{"name":"B"}]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_SingleEntryBonesArray_IsSilentlyIgnored()
    {
        const string json = """{"cnjVersion":1,"type":"Model","bones":[{"name":"Root"}],"meshes":[]}""";

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Empty(data.Meshes);
    }

    [Fact]
    public void Read_MeshWithMorphTargets_ThrowsContentLoadException()
    {
        const string json = """
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32,"morphTargets":[]}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Theory]
    [InlineData("""{"cnjVersion":1,"type":"Model","meshes":[{"name":"M","vertices":"","indices":"quad_idx.bin"}]}""")]
    [InlineData("""{"cnjVersion":1,"type":"Model","meshes":[{"name":"M","vertices":"quad_verts.bin","indices":""}]}""")]
    public void Read_MeshWithEmptySidecarField_IsSilentlySkipped(string json)
    {
        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Empty(data.Meshes);
    }

    [Fact]
    public void Read_MeshWithNonPositiveVertexStride_IsSilentlySkipped()
    {
        const string json = """
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":0}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Empty(data.Meshes);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(48)]
    [InlineData(52)]
    [InlineData(56)]
    [InlineData(68)]
    public void Read_MeshWithUnsupportedVertexStride_ThrowsContentLoadException(int stride)
    {
        string json = $$"""
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":{{stride}}}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Theory]
    [InlineData("SkinnedEffect")]
    [InlineData("PbrEffect")]
    [InlineData("DualTextureEffect")]
    public void Read_MeshWithUnsupportedEffect_ThrowsContentLoadException(string effectName)
    {
        string json = $$"""
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32,"effect":"{{effectName}}"}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Theory]
    [InlineData("../outside.bin")]
    [InlineData("/etc/passwd")]
    [InlineData("..\\..\\outside.bin")]
    public void Read_MeshWithPathEscapingContentRoot_ThrowsContentLoadException(string escapingPath)
    {
        string json = $$"""
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"{{escapingPath.Replace("\\", "\\\\")}}","indices":"quad_idx.bin","vertexStride":32}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_MeshWithVertexColorEnabledAndTexture_AppliesBothFields()
    {
        const string json = """
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32,"vertexColorEnabled":true,"texture":"quad_verts.bin"}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        CnjMeshData mesh = Assert.Single(data.Meshes);
        Assert.True(mesh.Effect.VertexColorEnabled);
        Assert.NotNull(mesh.Effect.TextureReference);
        Assert.Equal(Path.Combine(AssetsDirectory, "quad_verts.bin"), mesh.Effect.TextureReference);
    }

    [Fact]
    public void Read_MeshWithEmptyNameField_DefaultsToMesh()
    {
        const string json = """
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Equal("mesh", Assert.Single(data.Meshes).Name);
    }
}
