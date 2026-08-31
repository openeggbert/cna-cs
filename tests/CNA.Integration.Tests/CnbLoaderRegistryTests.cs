using CNA;
using CNA.Content.Cnb;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The CNB loader registry: how a game teaches CNA about its own <c>.cnb</c> asset types.
///
/// <b>This is the piece that makes the container format extensible</b>, and it is the CNB
/// counterpart of the <c>.xnb</c> reader table this binding already exposes through
/// <c>ContentTypeReaderRegistration</c> -- CNA's header says as much. Without it, the CNB surface
/// reads only what CNA itself knows how to build.
///
/// The round trip is complete and runs here end to end: mint an identifier from a type name, write a
/// container carrying both, register a loader, resolve the loader <em>from the file</em>, invoke it,
/// and get the managed object back.
///
/// <b>The registry is process-wide.</b> Every test disposes its registration, and each uses its own
/// type name so that a leak in one cannot make another pass by accident.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbLoaderRegistryTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>What a game's own asset deserialises into. Deliberately carries the bytes it was
    /// built from, so a test can tell one produced object from another.</summary>
    private sealed class Level(string name, byte[] payload)
    {
        public string Name { get; } = name;

        public byte[] Payload { get; } = payload;
    }

    private const string LevelChunk = "LVL_";

    private sealed class LevelLoader : CnbAssetLoader
    {
        public int Calls { get; private set; }

        public string? LastAssetName { get; private set; }

        public override object Load(CnbDocument document, string assetName)
        {
            Calls++;
            LastAssetName = assetName;

            // By chunk type, not by index. Measured: stamping the metadata adds a chunk of its own,
            // so the game's payload is not chunk zero -- and a loader indexing blindly reads CNA's
            // metadata as its own data, which is what the first version of this did.
            for (int index = 0; index < document.ChunkCount; index++)
            {
                if (document.GetChunk(index).TypeName == LevelChunk)
                {
                    return new Level(assetName, document.ReadChunkData(index));
                }
            }

            throw new InvalidOperationException($"'{assetName}' carries no {LevelChunk} chunk.");
        }
    }

    /// <summary>A loader that fails, to check the error channel.</summary>
    private sealed class ThrowingLoader : CnbAssetLoader
    {
        public override object Load(CnbDocument document, string assetName) =>
            throw new InvalidOperationException("this loader always fails");
    }

    private static string WriteCustomAsset(string typeName, byte[] payload, out uint assetTypeId)
    {
        assetTypeId = CnbLoaderRegistration.AssetTypeIdFromName(typeName);

        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-custom-{Guid.NewGuid():N}.cnb");
        using var writer = new CnbTestWriter(assetTypeId, 1);
        writer.SetMetadata(typeName, "levels/one");
        writer.AddChunk(CnbTestWriter.ChunkId(LevelChunk), payload);
        writer.WriteToFile(path);
        return path;
    }

    /// <summary>
    /// The identifier is a hash of the name, not a number a game picks.
    ///
    /// Both halves are asserted. It is stable, so a game that ships an asset today can register for
    /// it tomorrow; and it is in CNA's custom range, which is what stops a game claiming an
    /// identifier CNA owns. Two different names produce two different identifiers, so the derivation
    /// is a function of the name rather than a constant.
    /// </summary>
    [NativeFact]
    public void AssetTypeId_IsAStableHashOfTheNameInTheCustomRange()
    {
        fixture.InsideAFrame(_ =>
        {
            const uint CustomRangeFirst = 0x80000000;

            uint first = CnbLoaderRegistration.AssetTypeIdFromName("CnaCs.Tests.StableName");
            uint again = CnbLoaderRegistration.AssetTypeIdFromName("CnaCs.Tests.StableName");
            uint other = CnbLoaderRegistration.AssetTypeIdFromName("CnaCs.Tests.OtherName");

            Assert.Equal(first, again);
            Assert.NotEqual(first, other);
            Assert.True(first >= CustomRangeFirst, $"0x{first:X8} is outside CNA's custom range.");
            Assert.True(other >= CustomRangeFirst, $"0x{other:X8} is outside CNA's custom range.");

            output.WriteLine($"'CnaCs.Tests.StableName' -> 0x{first:X8}, 'CnaCs.Tests.OtherName' -> 0x{other:X8}");
        });
    }

    /// <summary>
    /// The whole round trip: a file CNA wrote, a loader this test registered, and the object it
    /// produced.
    ///
    /// The loader is resolved <b>from the document</b> rather than by identifier, which is the route
    /// a content path actually takes -- the file says what it is, and the registry either knows that
    /// type or does not.
    /// </summary>
    [NativeFact]
    public void RegisteredLoader_ResolvesFromTheFileAndProducesItsObject()
    {
        fixture.InsideAFrame(game =>
        {
            const string TypeName = "CnaCs.Tests.RoundTripLevel";
            byte[] payload = [9, 8, 7, 6, 5];
            string path = WriteCustomAsset(TypeName, payload, out uint assetTypeId);

            var loader = new LevelLoader();
            try
            {
                using var registration = CnbLoaderRegistration.Register(TypeName, loader);
                Assert.Equal(assetTypeId, registration.AssetTypeId);
                Assert.True(CnbLoaderRegistration.IsRegistered(assetTypeId));
                Assert.Equal(TypeName, CnbLoaderRegistration.RegisteredTypeName(assetTypeId));

                using CnbDocument document = CnbDocument.Open(path);
                Assert.Equal(assetTypeId, document.AssetTypeId);

                using CnbLoader resolved = CnbLoaderRegistration.ResolveFor(document);
                object? produced = resolved.Invoke(document, game.Content, "levels/one");

                Level level = Assert.IsType<Level>(produced);
                Assert.Equal("levels/one", level.Name);

                // Byte for byte. A loader handed the wrong chunk, or a document wrapper reading a
                // different container, produces a correctly sized array of the wrong bytes.
                Assert.Equal(payload, level.Payload);

                Assert.Equal(1, loader.Calls);
                Assert.Equal("levels/one", loader.LastAssetName);
                output.WriteLine($"loaded a {level.Payload.Length}-byte custom asset through the registry");
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>
    /// Disposing the registration withdraws the loader, and the file then resolves to nothing.
    ///
    /// The registry is process-wide, so this is the assertion that keeps these tests from leaking
    /// into each other -- and it is the one a game depends on when it unloads a mod.
    /// </summary>
    [NativeFact]
    public void DisposingTheRegistration_WithdrawsTheLoader()
    {
        fixture.InsideAFrame(_ =>
        {
            const string TypeName = "CnaCs.Tests.WithdrawnLevel";
            string path = WriteCustomAsset(TypeName, [1, 2, 3], out uint assetTypeId);

            try
            {
                Assert.False(CnbLoaderRegistration.IsRegistered(assetTypeId));

                var registration = CnbLoaderRegistration.Register(TypeName, new LevelLoader());
                Assert.True(CnbLoaderRegistration.IsRegistered(assetTypeId));
                Assert.NotNull(CnbLoader.Find(assetTypeId));

                registration.Dispose();

                Assert.False(CnbLoaderRegistration.IsRegistered(assetTypeId));
                Assert.Null(CnbLoader.Find(assetTypeId));

                using CnbDocument document = CnbDocument.Open(path);
                CnaException failure =
                    Assert.Throws<CnaException>(() => CnbLoaderRegistration.ResolveFor(document));
                output.WriteLine($"after withdrawal: {failure.Message}");
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>
    /// Registering the same type name twice is accepted and <b>replaces</b> the first loader -- and
    /// the two registrations then share one native slot, so disposing either withdraws it.
    ///
    /// Measured, and not what was expected. The header says an identifier "already registered under
    /// a different name" is refused, which reads as though a duplicate is refused generally; the
    /// same name is not. That distinction is exactly right for the collision case it is defending
    /// against -- two games can only share an identifier by sharing a type name -- but it leaves a
    /// managed hazard the header does not mention, and this pins it: <see cref="CnbLoaderRegistration.Dispose"/> removes
    /// by identifier, so disposing a replacement leaves the earlier registration object alive with
    /// nothing registered behind it.
    /// </summary>
    [NativeFact]
    public void RegisteringTheSameNameTwice_ReplacesAndSharesOneSlot()
    {
        fixture.InsideAFrame(_ =>
        {
            const string TypeName = "CnaCs.Tests.ReplacedLevel";

            var first = CnbLoaderRegistration.Register(TypeName, new LevelLoader());
            try
            {
                Assert.Equal(TypeName, CnbLoaderRegistration.RegisteredTypeName(first.AssetTypeId));

                CnbLoaderRegistration second = CnbLoaderRegistration.Register(TypeName, new LevelLoader());
                Assert.Equal(first.AssetTypeId, second.AssetTypeId);
                Assert.True(CnbLoaderRegistration.IsRegistered(first.AssetTypeId));

                // One slot, two owners. Disposing the replacement empties it, and the first
                // registration object cannot tell.
                second.Dispose();
                Assert.False(CnbLoaderRegistration.IsRegistered(first.AssetTypeId));
                Assert.Equal(string.Empty, CnbLoaderRegistration.RegisteredTypeName(first.AssetTypeId));

                output.WriteLine("same-name re-registration replaces; the two share one native slot");
            }
            finally
            {
                first.Dispose();
            }
        });
    }

    /// <summary>
    /// A container whose declared type name does not hash to its own identifier cannot be written at
    /// all.
    ///
    /// This test was written to check that such a file is refused at <em>load</em> time, and
    /// discovered something better: CNA's writer refuses to produce one, naming the hash collision
    /// it would cause. So the mismatch is unreachable through CNA's own tooling rather than merely
    /// survivable, and the assertion moved to where the refusal actually happens.
    /// </summary>
    [NativeFact]
    public void AContainerWhoseNameDoesNotHashToItsIdentifier_CannotBeWritten()
    {
        fixture.InsideAFrame(_ =>
        {
            uint honest = CnbLoaderRegistration.AssetTypeIdFromName("CnaCs.Tests.HonestLevel");
            string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-liar-{Guid.NewGuid():N}.cnb");

            try
            {
                using var writer = new CnbTestWriter(honest, 1);
                writer.SetMetadata("CnaCs.Tests.SomeOtherType", "levels/liar");
                writer.AddChunk(CnbTestWriter.ChunkId(LevelChunk), [1]);

                CnaException failure = Assert.Throws<CnaException>(() => writer.WriteToFile(path));
                Assert.Equal("Io", failure.NativeResult);
                Assert.Contains("hash collision", failure.Message, StringComparison.Ordinal);
                Assert.False(File.Exists(path), "A refused write must leave no file behind.");

                output.WriteLine($"the writer refuses it: {failure.Message}");
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>
    /// A loader that throws fails the load rather than unwinding into C.
    ///
    /// An exception crossing an <c>UnmanagedCallersOnly</c> boundary is undefined behaviour, so the
    /// callback catches. What this asserts is that catching does not turn a failure into a success:
    /// a binding that swallowed the exception and returned <c>Success</c> would hand the caller a
    /// null asset from a load that looked like it worked.
    ///
    /// <b>The result code is CNA's, not the callback's.</b> The callback answers <c>Callback</c>
    /// and <c>cna_cnb_loader_invoke</c> reports <c>Io</c> to its caller -- measured, after this test
    /// asserted <c>Callback</c> and was told otherwise. That is the right shape: from the caller's
    /// side an asset failed to load, and the message says which registered loader failed, which is
    /// the part that makes it diagnosable.
    /// </summary>
    [NativeFact]
    public void ALoaderThatThrows_FailsTheLoadRatherThanCrossingIntoC()
    {
        fixture.InsideAFrame(game =>
        {
            const string TypeName = "CnaCs.Tests.ThrowingLevel";
            string path = WriteCustomAsset(TypeName, [4, 4, 4], out uint _);

            try
            {
                using var registration = CnbLoaderRegistration.Register(TypeName, new ThrowingLoader());
                using CnbDocument document = CnbDocument.Open(path);
                using CnbLoader resolved = CnbLoaderRegistration.ResolveFor(document);

                CnaException failure = Assert.Throws<CnaException>(
                    () => resolved.Invoke(document, game.Content, "levels/broken"));
                Assert.Equal("Io", failure.NativeResult);

                // The message names the registration, which is what turns "an asset failed" into
                // something a game can act on. A failure reported without it would satisfy any test
                // that only checked the result code.
                Assert.Contains(TypeName, failure.Message, StringComparison.Ordinal);
                output.WriteLine($"a throwing loader fails the load: {failure.Message}");
            }
            finally
            {
                File.Delete(path);
            }
        });
    }
}
