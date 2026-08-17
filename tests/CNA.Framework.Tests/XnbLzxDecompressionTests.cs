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

    [Theory]
    [InlineData("Explosion")]
    [InlineData("FontCalibri14")]
    public void Decompress_RealFixture_MatchesIndependentReferenceOutputByteForByte(string fixtureName)
    {
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(AssetsDirectory, fixtureName + ".xnb"));
        byte[] expected = File.ReadAllBytes(Path.Combine(AssetsDirectory, "reference-decompressed", fixtureName + ".decompressed.bin"));

        using var reader = new BinaryReader(new MemoryStream(fileBytes));
        XnbHeader header = XnbHeader.Read(reader, fileBytes.Length);
        Assert.Equal(XnbCompression.Lzx, header.Compression);

        int decompressedSize = reader.ReadInt32();
        int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
        byte[] compressed = reader.ReadBytes(compressedSize);

        byte[] actual = XnbLzxDecompression.Decompress(compressed, decompressedSize, fixtureName);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Decompress_RealMultiBlockFixture_ExercisesMoreThanOneLzxBlock()
    {
        // FontCalibri14.xnb's decompressed size (44032 bytes) spans more than one 32KB LZX block
        // (32768 + 11264) -- genuinely exercises the block-framing loop's multi-block state
        // persistence (the same LzxDecoder instance's sliding window and repeated-offset LRU queue
        // must carry over correctly from the first Decompress() call into the second). This is a
        // sanity check on the fixture's own shape, not a new decompression assertion -- the byte-exact
        // check above already covers correctness.
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(AssetsDirectory, "FontCalibri14.xnb"));
        using var reader = new BinaryReader(new MemoryStream(fileBytes));
        XnbHeader header = XnbHeader.Read(reader, fileBytes.Length);
        int decompressedSize = reader.ReadInt32();

        Assert.True(decompressedSize > 0x8000);
    }

    [Theory]
    [InlineData("Explosion")]
    [InlineData("FontCalibri14")]
    public void FullParse_RealCompressedFixture_DecompressesThenFailsCleanlyOnUnsupportedRootType(string fixtureName)
    {
        // End-to-end integration check, mirroring XnbModelReaderTests's own
        // FullParse_RealFixture_ProducesExpectedModelData for the uncompressed path: drives the
        // exact same header-read -> LZX-decompress -> XnbContentReader.Create -> dispatch sequence
        // ContentManager.LoadXnbModelData uses internally (that method itself needs a real
        // ContentManager/RootDirectory, which needs native, so it's replicated here directly). Both
        // fixtures are real Texture2D/SpriteFont assets, not Model -- this project's .xnb reader only
        // registers Model-family type readers, so a *successful* decompression followed by a *clean*
        // ContentLoadException (naming the unsupported reader) confirms decompression produced
        // well-formed content bytes without silently corrupting anything downstream, not a crash or
        // a hang.
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(AssetsDirectory, fixtureName + ".xnb"));
        using var reader = new BinaryReader(new MemoryStream(fileBytes));
        XnbHeader header = XnbHeader.Read(reader, fileBytes.Length);

        int decompressedSize = reader.ReadInt32();
        int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
        byte[] compressed = reader.ReadBytes(compressedSize);
        byte[] decompressed = XnbLzxDecompression.Decompress(compressed, decompressedSize, fixtureName);

        XnbContentReader contentReader = XnbContentReader.Create(new BinaryReader(new MemoryStream(decompressed)));

        Assert.Throws<ContentLoadException>(() => contentReader.ReadRootObjectAndResolveSharedResources());
    }
}
