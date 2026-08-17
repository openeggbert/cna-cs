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

    [Theory]
    [InlineData((byte)0x80, "Lzx")]
    [InlineData((byte)0x40, "Lz4")]
    public void XnbHeader_Read_CompressedFile_ThrowsContentLoadException(byte flags, string expectedSchemeName)
    {
        byte[] bytes = [(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5, flags, 10, 0, 0, 0];
        using var reader = new BinaryReader(new MemoryStream(bytes));

        ContentLoadException exception = Assert.Throws<ContentLoadException>(() => XnbHeader.Read(reader, bytes.Length));
        Assert.Contains(expectedSchemeName, exception.Message);
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

    /// <summary>Builds a minimal, hand-crafted <c>.xnb</c> payload (type-reader table + a
    /// zero-version entry per name + shared resource count + root object bytes), starting a
    /// <see cref="BinaryReader"/> positioned right after the (never-written) 10-byte header --
    /// <see cref="XnbContentReader.Create"/> doesn't touch header bytes at all, so this is
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
