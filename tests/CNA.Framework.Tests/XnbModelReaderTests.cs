using System.Text;
using CNA.Content;
using CNA.Content.Xnb;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="XnbHeader"/>/<see cref="XnbContentReader"/>/<see cref="XnbModelReader"/> and their
/// sibling readers are pure C#/BCL logic with zero native dependency -- unlike almost everything
/// else native-adjacent this project has built, this is fully, end-to-end testable, including
/// against a real, uncompressed, MonoGame-compiled <c>Model</c> asset
/// (<c>assets/xnb/BlenderDefaultCube.xnb</c> -- see that directory's own <c>README.md</c> for
/// provenance). <see cref="XnbModelBuilder"/> (the step that actually constructs native-backed
/// <c>VertexBuffer</c>/<c>IndexBuffer</c> instances) is deliberately not exercised here -- that
/// part needs a real <c>cna-native</c>, same as every other native-backed type in this project.
/// </summary>
public class XnbModelReaderTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "assets", "xnb", "BlenderDefaultCube.xnb");

    [Fact]
    public void XnbHeader_Read_RealFixture_ParsesCorrectly()
    {
        using FileStream stream = File.OpenRead(FixturePath);
        using var reader = new BinaryReader(stream);

        XnbHeader header = XnbHeader.Read(reader, stream.Length);

        Assert.Equal('w', header.Platform);
        Assert.Equal(5, header.Version);
        Assert.Equal(XnbCompression.None, header.Compression);
        Assert.Equal(1802, header.TotalLength);
    }

    [Fact]
    public void FullParse_RealFixture_ProducesExpectedModelData()
    {
        using FileStream stream = File.OpenRead(FixturePath);
        using var reader = new BinaryReader(stream);

        _ = XnbHeader.Read(reader, stream.Length);
        XnbContentReader contentReader = XnbContentReader.Create(reader);
        object? root = contentReader.ReadRootObjectAndResolveSharedResources();

        var modelData = Assert.IsType<XnbModelData>(root);
        Assert.Equal(2, modelData.Bones.Count);
        Assert.Equal("RootNode", modelData.Bones[0].Name);

        // Confirmed by the research this reader was built from: this fixture's shared-resource
        // count is exactly 3 (one mesh part's VertexBuffer + IndexBuffer + Effect) -- i.e. exactly
        // one mesh part exists across every mesh in the file.
        XnbMeshPartData[] allParts = [.. modelData.Meshes.SelectMany(mesh => mesh.Parts)];
        XnbMeshPartData part = Assert.Single(allParts);

        Assert.NotNull(part.VertexBuffer);
        Assert.NotNull(part.IndexBuffer);
        Assert.NotNull(part.Effect);
        Assert.True(part.NumVertices > 0);
        Assert.True(part.PrimitiveCount > 0);

        Assert.True(part.VertexBuffer.Declaration.VertexStride > 0);
        Assert.Equal(part.VertexBuffer.VertexCount * part.VertexBuffer.Declaration.VertexStride, part.VertexBuffer.Data.Length);

        int indexSize = part.IndexBuffer.SixteenBits ? 2 : 4;
        Assert.Equal(0, part.IndexBuffer.Data.Length % indexSize);
    }

    [Theory]
    [InlineData((byte)'Y', (byte)'N', (byte)'B')]
    [InlineData((byte)'X', (byte)'Y', (byte)'B')]
    [InlineData((byte)'X', (byte)'N', (byte)'Y')]
    public void XnbHeader_Read_BadMagic_ThrowsContentLoadException(byte b0, byte b1, byte b2)
    {
        byte[] bytes = [b0, b1, b2, (byte)'w', 5, 0, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
    }

    [Theory]
    [InlineData((byte)3)]
    [InlineData((byte)6)]
    [InlineData((byte)0)]
    public void XnbHeader_Read_UnsupportedVersion_ThrowsContentLoadException(byte version)
    {
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', version, 0, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
    }

    [Fact]
    public void XnbHeader_Read_TotalLengthMismatch_ThrowsContentLoadException()
    {
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5, 0, 99, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
    }

    [Fact]
    public void XnbHeader_Read_Lz4CompressedFile_ThrowsContentLoadException()
    {
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5, 0x40, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        ContentLoadException exception = Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
        Assert.Contains("Lz4", exception.Message);
    }

    [Fact]
    public void XnbHeader_Read_LzxCompressedFile_ReturnsLzxCompression()
    {
        // Lzx is a real, supported value -- unlike Lz4, XnbHeader.Read no longer rejects it (real
        // decompression happens downstream, in XnbLzxDecompression/LzxDecoder).
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5, 0x80, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        XnbHeader header = XnbHeader.Read(reader, bytes.Length);

        Assert.Equal(XnbCompression.Lzx, header.Compression);
    }

    [Fact]
    public void XnbHeader_Read_BothCompressionBitsSet_ThrowsContentLoadException()
    {
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5, 0xC0, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
    }

    [Fact]
    public void ReadObject_NullIndex_ReturnsNull()
    {
        XnbContentReader reader = CreateReader(typeReaderNames: [], sharedResourceCount: 0, writeRoot: w => w.Write7BitEncodedInt(0));

        object? result = reader.ReadRootObjectAndResolveSharedResources();

        Assert.Null(result);
    }

    [Fact]
    public void ReadObject_OutOfRangeIndex_ThrowsContentLoadException()
    {
        XnbContentReader reader = CreateReader(typeReaderNames: [], sharedResourceCount: 0, writeRoot: w => w.Write7BitEncodedInt(5));

        Assert.Throws<ContentLoadException>(() => reader.ReadRootObjectAndResolveSharedResources());
    }

    [Fact]
    public void ReadObject_UnsupportedTypeReader_ThrowsContentLoadException()
    {
        XnbContentReader reader = CreateReader(
            typeReaderNames: ["Microsoft.Xna.Framework.Content.SomeReaderThisProjectDoesNotSupport"],
            sharedResourceCount: 0,
            writeRoot: w => w.Write7BitEncodedInt(1));

        Assert.Throws<ContentLoadException>(() => reader.ReadRootObjectAndResolveSharedResources());
    }

    [Fact]
    public void ReadObject_StringReader_ReadsRealString()
    {
        XnbContentReader reader = CreateReader(
            typeReaderNames: ["Microsoft.Xna.Framework.Content.StringReader"],
            sharedResourceCount: 0,
            writeRoot: w =>
            {
                w.Write7BitEncodedInt(1);
                w.Write("Hello");
            });

        object? result = reader.ReadRootObjectAndResolveSharedResources();

        Assert.Equal("Hello", result);
    }

    [Theory]
    [InlineData(true, 3)] // 3 is not a whole number of 2-byte (sixteen-bit) indices
    [InlineData(false, 6)] // 6 is not a whole number of 4-byte (thirty-two-bit) indices
    public void XnbIndexBufferReader_Read_SizeNotWholeNumberOfIndices_ThrowsContentLoadException(bool sixteenBits, int dataSize)
    {
        // Regression test (code review finding): a size that doesn't evenly divide by the index
        // element size would previously reach XnbModelBuilder.BuildIndexBuffer, where integer
        // division silently truncated the element count -- allocating a native IndexBuffer smaller
        // than the byte array later written into it. Rejected at the reader instead.
        XnbContentReader reader = CreateReader(
            typeReaderNames: [],
            sharedResourceCount: 0,
            writeRoot: w =>
            {
                w.Write(sixteenBits);
                w.Write(dataSize);
                w.Write(new byte[dataSize]);
            });

        Assert.Throws<ContentLoadException>(() => XnbIndexBufferReader.Read(reader));
    }

    [Fact]
    public void ReadBoneReference_OutOfRangeIndex_ThrowsContentLoadException()
    {
        // Regression test (code review finding): an out-of-range bone reference previously reached
        // XnbModelBuilder's own bones[index] lookups unchecked, risking an unhandled
        // ArgumentOutOfRangeException instead of this feature's usual ContentLoadException contract
        // for corrupt files.
        XnbContentReader reader = CreateReader(typeReaderNames: [], sharedResourceCount: 0, writeRoot: w => w.Write((byte)200));

        Assert.Throws<ContentLoadException>(() => reader.ReadBoneReference(boneCount: 2));
    }

    [Fact]
    public void ReadBoneReference_ValidIndex_ReturnsZeroBasedIndex()
    {
        XnbContentReader reader = CreateReader(typeReaderNames: [], sharedResourceCount: 0, writeRoot: w => w.Write((byte)2));

        Assert.Equal(1, reader.ReadBoneReference(boneCount: 2));
    }

    [Fact]
    public void XnbModelReader_Read_ImplausibleBoneCount_ThrowsContentLoadException()
    {
        // Regression test (code review finding): boneCount had no plausibility bound, unlike the
        // type-reader-table count and vertex element count elsewhere in this feature.
        XnbContentReader reader = CreateReader(typeReaderNames: [], sharedResourceCount: 0, writeRoot: w => w.Write(2_000_000_000u));

        Assert.Throws<ContentLoadException>(() => XnbModelReader.Read(reader));
    }

    /// <summary>
    /// A model's <c>Tag</c> is stored, and so are its meshes' and mesh parts'.
    ///
    /// This reader used to refuse any non-null tag, on the premise that "real content pipeline
    /// output never actually sets one". 28 assets in the XNA 4.0 sample collection do, always a
    /// <c>Dictionary&lt;string, object&gt;</c>, and every one of them failed to load. The three
    /// slots carry different values here, because a reader that stored one tag in all three, or
    /// read them in the wrong order, would pass a test that used the same value everywhere.
    /// </summary>
    [Fact]
    public void Tags_AreStoredForTheModelItsMeshAndItsMeshPart()
    {
        XnbContentReader reader = CreateReader(
            typeReaderNames:
            [
                "Microsoft.Xna.Framework.Content.StringReader",
                "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]",
                "Microsoft.Xna.Framework.Content.Int32Reader",
            ],
            sharedResourceCount: 0,
            writeRoot: w =>
            {
                w.Write(1u);                                // one bone
                w.Write7BitEncodedInt(1); w.Write("Root");  // its name
                for (int i = 0; i < 16; i++) { w.Write(i == 0 || i == 5 || i == 10 || i == 15 ? 1f : 0f); }
                w.Write((byte)0);                           // the bone's parent: none
                w.Write(0u);                                // no children

                w.Write(1);                                 // one mesh
                w.Write7BitEncodedInt(1); w.Write("Mesh");
                w.Write((byte)1);                           // parent bone reference (1-based)
                w.Write(0f); w.Write(0f); w.Write(0f); w.Write(1f);  // bounding sphere

                // The mesh's tag: a dictionary with one entry.
                w.Write7BitEncodedInt(2);
                w.Write(1);
                w.Write7BitEncodedInt(1); w.Write("meshKey");
                w.Write7BitEncodedInt(3); w.Write(7);

                w.Write(1);                                 // one mesh part
                w.Write(0); w.Write(3); w.Write(0); w.Write(1);
                w.Write7BitEncodedInt(1); w.Write("partTag"); // the part's tag: a plain string
                w.Write7BitEncodedInt(0);                   // VertexBuffer: no shared resource
                w.Write7BitEncodedInt(0);                   // IndexBuffer
                w.Write7BitEncodedInt(0);                   // Effect

                w.Write((byte)1);                           // root bone reference

                // The model's own tag: a different dictionary.
                w.Write7BitEncodedInt(2);
                w.Write(1);
                w.Write7BitEncodedInt(1); w.Write("modelKey");
                w.Write7BitEncodedInt(3); w.Write(42);
            });

        var model = Assert.IsType<XnbModelData>(XnbModelReader.Read(reader));

        var modelTag = Assert.IsType<Dictionary<string, object>>(model.Tag);
        Assert.Equal(42, modelTag["modelKey"]);

        var meshTag = Assert.IsType<Dictionary<string, object>>(model.Meshes[0].Tag);
        Assert.Equal(7, meshTag["meshKey"]);

        Assert.Equal("partTag", model.Meshes[0].Parts[0].Tag);
    }

    /// <summary>Builds a minimal, hand-crafted <c>.xnb</c> payload (type-reader table + a
    /// zero-version entry per name + shared resource count + root object bytes), starting a
    /// <see cref="BinaryReader"/> positioned right after the (never-written) 10-byte header --
    /// <see cref="XnbContentReader.Create(BinaryReader, string)"/> doesn't touch header bytes at all, so this is
    /// sufficient without also constructing a real <see cref="XnbHeader"/>.</summary>
    private static XnbContentReader CreateReader(string[] typeReaderNames, int sharedResourceCount, Action<BinaryWriter> writeRoot)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write7BitEncodedInt(typeReaderNames.Length);
        foreach (string name in typeReaderNames)
        {
            writer.Write(name);
            writer.Write(0);
        }

        writer.Write7BitEncodedInt(sharedResourceCount);
        writeRoot(writer);
        writer.Flush();

        stream.Position = 0;
        var reader = new BinaryReader(stream);
        return XnbContentReader.Create(reader);
    }
}
