using CNA.Content.Cnb;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D2's first vertical slice: CNA's own binary content container, opened and read.
///
/// The fixture is written by CNA's own encoder at test time rather than vendored. That matters for
/// what the test proves: a byte array assembled here from a reading of the format would check this
/// repository's understanding against itself, and would keep passing if both the reader and the
/// hand-built fixture were wrong in the same way. A file CNA wrote cannot agree with a mistake this
/// side made.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbDocumentTests(ITestOutputHelper output)
{
    private const uint AssetTypeId = 0x54534554;      // 'TEST'
    private const uint SchemaVersion = 3;

    [NativeFact]
    public void WrittenDocument_ReadsBackWhatWasWritten()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-{Guid.NewGuid():N}.cnb");
        byte[] first = [1, 2, 3, 4, 5, 6, 7, 8];
        byte[] second = [0xAA, 0xBB, 0xCC];

        try
        {
            using (var writer = new CnbTestWriter(AssetTypeId, SchemaVersion))
            {
                writer.AddChunk(CnbTestWriter.ChunkId("ONE_"), first);
                writer.AddChunk(CnbTestWriter.ChunkId("TWO_"), second);
                writer.WriteToFile(path);
            }

            Assert.True(File.Exists(path), "CNA's encoder reported success but wrote no file.");

            using var document = CnbDocument.Open(path);

            output.WriteLine(
                $"container {document.ContainerMajorVersion}.{document.ContainerMinorVersion}, " +
                $"asset type 0x{document.AssetTypeId:X8} schema {document.AssetSchemaVersion}, " +
                $"{document.ChunkCount} chunk(s)");

            // The identity the writer was given has to survive the round trip. Asserting the
            // container version only would pass for a file describing something else entirely.
            Assert.Equal(AssetTypeId, document.AssetTypeId);
            Assert.Equal(SchemaVersion, document.AssetSchemaVersion);
            Assert.True(document.ContainerMajorVersion >= 1, "A written container must declare a version.");

            // Chunks the writer did not add may exist -- the encoder writes its own -- so the test
            // asserts that the two it did add are present and correct, not that they are the only
            // ones. Demanding an exact count would be asserting the encoder's private layout.
            var byName = new Dictionary<string, CnbChunk>(StringComparer.Ordinal);
            for (int index = 0; index < document.ChunkCount; index++)
            {
                CnbChunk chunk = document.GetChunk(index);
                byName[chunk.TypeName] = chunk;
                output.WriteLine($"  [{index}] {chunk.TypeName} stored={chunk.StoredSize} logical={chunk.UncompressedSize}");
            }

            Assert.True(byName.ContainsKey("ONE_"), $"chunk ONE_ is missing; found {string.Join(", ", byName.Keys)}");
            Assert.True(byName.ContainsKey("TWO_"), $"chunk TWO_ is missing; found {string.Join(", ", byName.Keys)}");
            Assert.Equal((ulong)first.Length, byName["ONE_"].UncompressedSize);
            Assert.Equal((ulong)second.Length, byName["TWO_"].UncompressedSize);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The bytes come back, not merely their length.
    ///
    /// A reader that returned a correctly sized array of zeros would satisfy every size assertion
    /// above, so the payload is compared element by element against what was written.
    /// </summary>
    [NativeFact]
    public void ChunkData_RoundTripsByteForByte()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-{Guid.NewGuid():N}.cnb");
        byte[] payload = [.. Enumerable.Range(0, 256).Select(i => (byte)(i * 7 % 251))];

        try
        {
            using (var writer = new CnbTestWriter(AssetTypeId, SchemaVersion))
            {
                writer.AddChunk(CnbTestWriter.ChunkId("DATA"), payload);
                writer.WriteToFile(path);
            }

            using var document = CnbDocument.Open(path);

            int index = Enumerable.Range(0, document.ChunkCount)
                .First(i => document.GetChunk(i).TypeName == "DATA");

            byte[] read = document.ReadChunkData(index);

            Assert.Equal(payload.Length, read.Length);
            Assert.Equal(payload, read);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A file that is not a container is refused, rather than parsed into something
    /// plausible.</summary>
    [NativeFact]
    public void NotAContainer_IsRefused()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-{Guid.NewGuid():N}.cnb");
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5, 6, 7]);

        try
        {
            CNA.CnaException thrown = Assert.Throws<CNA.CnaException>(() => CnbDocument.Open(path));
            output.WriteLine($"refused with {thrown.NativeResult}: {thrown.Message}");
            Assert.NotNull(thrown.NativeResult);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
