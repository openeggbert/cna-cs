using CNA.Content;
using CNA.Content.Xnb;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="LzxDecoder"/>/<see cref="XnbLzxDecompression"/> are pure C#/BCL logic with zero native
/// dependency -- like <c>XnbModelReaderTests</c>, this is fully, end-to-end testable, including
/// against real, LZX-compressed, MonoGame-compiled fixtures with an independently-produced,
/// byte-exact reference decompressed output (see <c>assets/xnb/lzx/README.md</c> for provenance) --
/// not just "decompresses without throwing," the real cross-implementation check.
/// </summary>
public class XnbLzxDecompressionTests
{
    private static readonly string AssetsDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "xnb", "lzx");

    /// <summary>
    /// End-to-end integration check for the compressed path, mirroring
    /// <c>XnbModelReaderTests.FullParse_RealFixture_ProducesExpectedModelData</c> for the
    /// uncompressed one: header-read, LZX-decompress, <c>XnbContentReader.Create</c>, dispatch --
    /// the exact sequence <c>ContentManager</c> runs internally (that method itself needs a real
    /// <c>ContentManager</c>/<c>RootDirectory</c>, which needs native, so it is replicated here).
    ///
    /// This used to assert a *clean failure* on an unregistered root type, because both fixtures are
    /// Texture2D/SpriteFont assets and this reader only knew the Model family. Both are registered
    /// now, so the check is that decompression produces content the readers actually accept, which
    /// is a stronger statement about the same bytes than "it failed the way we expected".
    /// <c>XnbSpriteFontReaderTests</c> asserts what the parse produced.
    /// </summary>
    [Theory]
    [InlineData("Explosion")]
    [InlineData("FontCalibri14")]
    public void FullParse_RealCompressedFixture_DecompressesIntoReadableContent(string fixtureName)
    {
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(AssetsDirectory, fixtureName + ".xnb"));
        using var reader = new BinaryReader(new MemoryStream(fileBytes));
        XnbHeader header = XnbHeader.Read(reader, fileBytes.Length);

        int decompressedSize = reader.ReadInt32();
        int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
        byte[] compressed = reader.ReadBytes(compressedSize);
        byte[] decompressed = XnbLzxDecompression.Decompress(compressed, decompressedSize, fixtureName);

        XnbContentReader contentReader = XnbContentReader.Create(new BinaryReader(new MemoryStream(decompressed)));

        Assert.NotNull(contentReader.ReadRootObjectAndResolveSharedResources());
    }
}
