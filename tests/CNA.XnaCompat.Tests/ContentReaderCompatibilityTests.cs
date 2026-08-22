using System.Text;
using Microsoft.Xna.Framework.Content;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// End-to-end custom XNB coverage for the public XNA reader contract. The fixture is deliberately
/// hand-written: it proves reader-table activation instead of accidentally relying on CNA's native
/// content type registration, and needs neither a graphics device nor a native library.
/// </summary>
public class ContentReaderCompatibilityTests
{
    [Fact]
    public void Load_CustomType_ActivatesReader_UsesExistingInstance_ResolvesSharedResource_AndRecordsDisposables()
    {
        TestContentReader.InitializationCount = 0;
        TestContentReader.ReceivedExistingInstance = false;
        TestContent.DisposeCount = 0;

        using var content = new MemoryContentManager(BuildCustomAsset());

        Envelope result = content.Load<Envelope>("custom");

        Assert.Equal(1, TestContentReader.InitializationCount);
        Assert.True(TestContentReader.ReceivedExistingInstance);
        Assert.Equal(17, result.Value.Value);
        Assert.NotNull(result.Value.Shared);
        Assert.Equal(23, result.Value.Shared!.Value);

        content.Unload();

        // The root and its one shared resource were both returned by a ContentTypeReader and are
        // therefore owned by ContentManager exactly as normal XNA content is.
        Assert.Equal(2, TestContent.DisposeCount);
    }

    private static byte[] BuildCustomAsset()
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write7BitEncodedInt(2);
            writer.Write(typeof(EnvelopeReader).AssemblyQualifiedName!);
            writer.Write(0);
            writer.Write(typeof(TestContentReader).AssemblyQualifiedName!);
            writer.Write(7);

            writer.Write7BitEncodedInt(1); // one deferred shared resource

            writer.Write7BitEncodedInt(1); // EnvelopeReader
            writer.Write7BitEncodedInt(2); // TestContentReader nested in the envelope
            writer.Write(17);
            writer.Write7BitEncodedInt(1); // reference shared resource #1

            writer.Write7BitEncodedInt(2); // TestContentReader for the shared resource
            writer.Write(23);
            writer.Write7BitEncodedInt(0); // no shared reference from the shared value
        }

        byte[] bytes = payload.ToArray();
        using var container = new MemoryStream();
        using (var writer = new BinaryWriter(container, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((byte)'X');
            writer.Write((byte)'N');
            writer.Write((byte)'B');
            writer.Write((byte)'w');
            writer.Write((byte)5);
            writer.Write((byte)0);
            writer.Write(10 + bytes.Length);
            writer.Write(bytes);
        }

        return container.ToArray();
    }

    private sealed class MemoryContentManager(byte[] asset) : ContentManager(new NullServiceProvider())
    {
        protected override Stream OpenStream(string assetName)
        {
            Assert.Equal("custom", assetName);
            return new MemoryStream(asset, writable: false);
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class Envelope
    {
        public required TestContent Value { get; init; }
    }

    private sealed class EnvelopeReader : ContentTypeReader<Envelope>
    {
        protected override Envelope Read(ContentReader input, Envelope existingInstance) => new()
        {
            Value = input.ReadObject(new TestContent { Value = -1 }),
        };
    }

    private sealed class TestContent : IDisposable
    {
        public static int DisposeCount { get; set; }

        public int Value { get; set; }

        public TestContent? Shared { get; set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class TestContentReader : ContentTypeReader<TestContent>
    {
        public static int InitializationCount { get; set; }

        public static bool ReceivedExistingInstance { get; set; }

        public override int TypeVersion => 7;

        protected override void Initialize(ContentTypeReaderManager manager)
        {
            Assert.Same(this, manager.GetTypeReader(typeof(TestContent)));
            InitializationCount++;
        }

        protected override TestContent Read(ContentReader input, TestContent existingInstance)
        {
            ReceivedExistingInstance |= existingInstance is not null;
            TestContent result = existingInstance ?? new TestContent();
            result.Value = input.ReadInt32();
            input.ReadSharedResource<TestContent>(shared => result.Shared = shared);
            return result;
        }
    }
}
