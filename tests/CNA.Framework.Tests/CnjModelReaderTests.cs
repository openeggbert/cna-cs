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
        // cnjVersion 2 is now supported (real "bones" hierarchy) -- version 3 stays unsupported.
        const string json = """{"cnjVersion":3,"type":"Model"}""";

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
    public void Read_MultiEntryBonesArray_ParsesRealHierarchy()
    {
        // cnjVersion 2's real "bones" scene-graph hierarchy -- three bones, parent-before-child
        // (Root -> Body -> Head), Head's transform a non-identity translation to confirm the
        // 16-element row-major float array is actually read, not just defaulted.
        const string json = """
            {"cnjVersion":2,"type":"Model","bones":[
                {"name":"Root"},
                {"name":"Body","parent":0},
                {"name":"Head","parent":1,"transform":[1,0,0,0, 0,1,0,0, 0,0,1,0, 0,2,0,1]}
            ],"meshes":[]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Equal(3, data.Bones.Count);
        Assert.Equal("Root", data.Bones[0].Name);
        Assert.Equal(-1, data.Bones[0].Parent);
        Assert.Equal("Body", data.Bones[1].Name);
        Assert.Equal(0, data.Bones[1].Parent);
        Assert.Equal("Head", data.Bones[2].Name);
        Assert.Equal(1, data.Bones[2].Parent);
        Assert.Equal(2f, data.Bones[2].Transform.M42);
    }

    [Fact]
    public void Read_BonesWithDefaultsOnly_UsesNameAndParentFallbacks()
    {
        // No "name"/"parent"/"transform" fields at all -- every default applies: entry 0 -> "Root",
        // later entries -> "Node{index}" and parent 0, transform -> identity.
        const string json = """{"cnjVersion":2,"type":"Model","bones":[{},{},{}],"meshes":[]}""";

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Equal("Root", data.Bones[0].Name);
        Assert.Equal("Node1", data.Bones[1].Name);
        Assert.Equal(0, data.Bones[1].Parent);
        Assert.Equal("Node2", data.Bones[2].Name);
        Assert.Equal(0, data.Bones[2].Parent);
        Assert.Equal(Matrix.Identity, data.Bones[2].Transform);
    }

    [Fact]
    public void Read_BoneWithOutOfRangeParent_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":2,"type":"Model","bones":[{"name":"A"},{"name":"B","parent":5}]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_BoneWithForwardReferenceParent_ThrowsContentLoadException()
    {
        // parent-before-child is a real invariant, not just a convention -- a bone referencing a
        // *later* entry (including itself) must be rejected, not silently accepted.
        const string json = """{"cnjVersion":2,"type":"Model","bones":[{"name":"A"},{"name":"B","parent":1}]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_BoneWithNonObjectEntry_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":2,"type":"Model","bones":["not an object","also not"]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_BoneWithWrongLengthTransform_ThrowsContentLoadException()
    {
        const string json = """{"cnjVersion":2,"type":"Model","bones":[{"name":"A"},{"name":"B","transform":[1,2,3]}]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_SingleEntryBonesArray_IsSilentlyIgnored()
    {
        const string json = """{"cnjVersion":1,"type":"Model","bones":[{"name":"Root"}],"meshes":[]}""";

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Empty(data.Bones);
        Assert.Empty(data.Meshes);
    }

    [Fact]
    public void Read_MeshWithParentBone_UsesRealHierarchy()
    {
        const string json = """
            {"cnjVersion":2,"type":"Model","bones":[
                {"name":"Root"},
                {"name":"Body","parent":0}
            ],"meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32,"parentBone":1}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Equal(1, Assert.Single(data.Meshes).ParentBoneIndex);
    }

    [Fact]
    public void Read_MeshWithoutParentBoneField_DefaultsToRootWhenHierarchyPresent()
    {
        const string json = """
            {"cnjVersion":2,"type":"Model","bones":[
                {"name":"Root"},
                {"name":"Body","parent":0}
            ],"meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Equal(0, Assert.Single(data.Meshes).ParentBoneIndex);
    }

    [Fact]
    public void Read_MeshWithNoBoneHierarchy_HasNullParentBoneIndex()
    {
        const string json = """
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32}
            ]}
            """;

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Null(Assert.Single(data.Meshes).ParentBoneIndex);
    }

    [Fact]
    public void Read_MeshWithOutOfRangeParentBone_ThrowsContentLoadException()
    {
        const string json = """
            {"cnjVersion":2,"type":"Model","bones":[
                {"name":"Root"},
                {"name":"Body","parent":0}
            ],"meshes":[
                {"name":"M","vertices":"quad_verts.bin","indices":"quad_idx.bin","vertexStride":32,"parentBone":9}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
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

    [Theory]
    [InlineData("truncated_verts.bin", "quad_idx.bin")] // vertex sidecar short by a few bytes
    [InlineData("quad_verts.bin", "truncated_idx.bin")] // index sidecar short by a few bytes
    public void Read_SidecarBytesNotWholeNumberOfElements_ThrowsContentLoadException(string verticesFile, string indicesFile)
    {
        // Regression test (code review finding, in two rounds): XnbVertexBufferData/
        // XnbIndexBufferData's own documented invariant is Data.Length == count * (stride|indexSize)
        // exactly. A first fix silently truncated a sidecar file whose byte length wasn't an exact
        // multiple of that to avoid overrunning the native buffer VertexBuffer/IndexBuffer.SetData
        // uploads into -- but a follow-up review finding pointed out that silently dropping real
        // geometry with no diagnostic is inconsistent with this reader's own "detect and throw"
        // discipline and with XnbIndexBufferReader's own identical precedent for the .xnb path,
        // which throws instead. Rejecting outright, as this test now confirms.
        string json = $$"""
            {"cnjVersion":1,"type":"Model","meshes":[
                {"name":"M","vertices":"{{verticesFile}}","indices":"{{indicesFile}}","vertexStride":32}
            ]}
            """;

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_NonArrayBonesField_ThrowsContentLoadException()
    {
        // Regression test (code review finding): a "bones" field present but not a JSON array (an
        // author mistake) was previously silently treated as "no bones" instead of being rejected,
        // diverging from every other malformed-field case in this reader.
        const string json = """{"cnjVersion":1,"type":"Model","bones":{"name":"Root"},"meshes":[]}""";

        Assert.Throws<ContentLoadException>(() => CnjModelReader.Read(json, "bad", AssetsDirectory));
    }

    [Fact]
    public void Read_NullBonesField_IsTreatedSameAsAbsent()
    {
        // Regression test (code review finding): the fix above for a non-array "bones" field
        // over-rejected the JSON literal null too -- a common "always emit optional keys" authoring
        // convention some serializers use instead of omitting the key entirely. null must stay
        // equivalent to "absent" (silently ignored), distinct from a genuinely wrong-typed value
        // like an object or a number.
        const string json = """{"cnjVersion":1,"type":"Model","bones":null,"meshes":[]}""";

        CnjModelData data = CnjModelReader.Read(json, "ok", AssetsDirectory);

        Assert.Empty(data.Meshes);
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
