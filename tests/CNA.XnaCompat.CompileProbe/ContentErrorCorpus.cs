using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace XnaCompatibilityCompileProbe;

/// <summary>
/// Tiny synthetic XNB error-path observations. Every fixture is generated in memory and contains
/// only this probe's reader type names, so the same source can run unchanged against Microsoft XNA,
/// CNA, FNA, and MonoGame without redistributing content or framework binaries.
/// </summary>
public static class ContentErrorCorpus
{
    public static IReadOnlyList<string> Capture()
    {
        var observations = new List<string>();
        byte[] valid = Container(IntPayload());

        Observe(observations, "content.bad_magic", () => Load<int>(Mutate(valid, 0, (byte)'Q')));
        Observe(observations, "content.bad_platform", () => Load<int>(Mutate(valid, 3, (byte)'x')));
        Observe(observations, "content.bad_version", () => Load<int>(Mutate(valid, 4, 4)));
        Observe(observations, "content.truncated_header", () => Load<int>([(byte)'X', (byte)'N', (byte)'B', (byte)'w', 5]));
        Observe(observations, "content.declared_size_larger", () => Load<int>(WithDeclaredSize(valid, valid.Length + 1)));
        Observe(observations, "content.declared_size_smaller", () => Load<int>(WithDeclaredSize(valid, valid.Length - 1)));
        Observe(observations, "content.profile_flag_40", () => Load<int>(Container(IntPayload(), flags: 0x40)));
        Observe(observations, "content.profile_flags_7f", () => Load<int>(Container(IntPayload(), flags: 0x7f)));
        Observe(observations, "content.truncated_compressed", () => Load<int>(Container([0, 0, 0, 0], flags: 0x80)));

        var truncatedBody = new byte[valid.Length - 1];
        Array.Copy(valid, truncatedBody, truncatedBody.Length);
        truncatedBody = WithDeclaredSize(truncatedBody, truncatedBody.Length);
        Observe(observations, "content.truncated_body", () => Load<int>(truncatedBody));
        Observe(observations, "content.unknown_reader", () => Load<int>(Container(Payload(
            "Missing.Reader, Missing.Assembly", 0, 0, writer => Write7Bit(writer, 1)))));
        Observe(observations, "content.reader_count_zero", () => Load<int>(Container(ReaderCountZeroPayload())));
        Observe(observations, "content.reader_count_too_large", () => Load<int>(Container(ReaderCountTooLargePayload())));
        Observe(observations, "content.reader_activation_failure", () => Load<int>(Container(Payload(
            typeof(ThrowingConstructorReader).AssemblyQualifiedName!, 0, 0,
            writer => Write7Bit(writer, 1)))));
        Observe(observations, "content.reader_version_mismatch", () => Load<int>(Container(IntPayload(serializedVersion: 0))));
        Observe(observations, "content.reader_index_out_of_range", () => Load<int>(Container(Payload(
            typeof(CorpusIntReader).AssemblyQualifiedName!, 1, 0, writer => Write7Bit(writer, 2)))));
        Observe(observations, "content.shared_index_out_of_range", () => Load<CorpusDisposable>(Container(Payload(
            typeof(SharedReferenceReader).AssemblyQualifiedName!, 0, 0, writer =>
            {
                Write7Bit(writer, 1);
                Write7Bit(writer, 1);
            }))));
        Observe(observations, "content.custom_reader_throw", () => Load<int>(Container(Payload(
            typeof(ThrowingReader).AssemblyQualifiedName!, 0, 0, writer => Write7Bit(writer, 1)))));
        Observe(observations, "content.wrong_target", () => Load<string>(valid));
        Observe(observations, "content.external_reference", () => LoadExternalReference(empty: false));
        Observe(observations, "content.external_reference_empty", () => LoadExternalReference(empty: true));
        Observe(observations, "content.missing_asset", LoadMissingAsset);

        observations.Add("content.open_stream_disposed=" + Flag(StreamIsDisposedAfterFailure()));
        observations.Add("content.partial_failure_unload=" + PartialFailureUnload());
        observations.Add("content.duplicate_disposable=" + DuplicateDisposable());
        observations.Add("content.unload_throw_clears=" + UnloadThrowClears());
        observations.Add("content.dispose_throw_poisons=" + DisposeThrowPoisons());

        return observations;
    }

    private static int Load<T>(byte[] asset)
    {
        using var content = new MemoryContentManager(() => new TrackingStream(asset));
        object? value = content.Load<T>("fixture");
        return value is int number ? number : 0;
    }

    private static int LoadExternalReference(bool empty)
    {
        byte[] root = Container(Payload(
            typeof(ExternalReferenceReader).AssemblyQualifiedName!, 0, 0,
            writer =>
            {
                Write7Bit(writer, 1);
                writer.Write(empty ? string.Empty : "child");
            }));
        var assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["folder/root"] = root,
            ["folder/child"] = Container(IntPayload()),
        };
        using var content = new MappedContentManager(assets);
        return content.Load<int>("folder/root");
    }

    private static int LoadMissingAsset()
    {
        using var content = new ContentManager(
            new NullServiceProvider(), "__cna_missing_content_fixture__");
        return content.Load<int>("absent");
    }

    private static bool StreamIsDisposedAfterFailure()
    {
        TrackingStream? stream = null;
        using var content = new MemoryContentManager(() => stream = new TrackingStream([0]));
        try
        {
            content.Load<int>("fixture");
        }
        catch
        {
        }

        return stream is { WasDisposed: true };
    }

    private static string PartialFailureUnload()
    {
        CorpusDisposable.Reset();
        byte[] asset = Container(Payload(
            typeof(NewDisposableReader).AssemblyQualifiedName!, 0, 1,
            writer => Write7Bit(writer, 1))); // root exists; the declared shared object is absent
        using var content = new MemoryContentManager(() => new TrackingStream(asset));
        string load = ExceptionName(() => content.Load<CorpusDisposable>("fixture"));
        content.Unload();
        return $"{load}/{CorpusDisposable.DisposeCount.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string DuplicateDisposable()
    {
        CorpusDisposable.Reset();
        SingletonDisposableReader.Reset();
        byte[] asset = Container(Payload(
            typeof(SingletonDisposableReader).AssemblyQualifiedName!, 0, 1,
            writer =>
            {
                Write7Bit(writer, 1);
                Write7Bit(writer, 1);
            }));
        using var content = new MemoryContentManager(() => new TrackingStream(asset));
        content.Load<CorpusDisposable>("fixture");
        content.Unload();
        return CorpusDisposable.DisposeCount.ToString(CultureInfo.InvariantCulture);
    }

    private static string UnloadThrowClears()
    {
        SequencedDisposableReader.Reset();
        byte[] asset = SequencedDisposableAsset();
        var content = new MemoryContentManager(() => new TrackingStream(asset));
        try
        {
            content.Load<CorpusDisposable>("fixture");
            string first = ExceptionName(content.Unload);
            string second = ExceptionName(content.Unload);
            return $"{first}/{second}/{SequencedDisposableReader.FirstDisposeCount}/" +
                SequencedDisposableReader.SecondDisposeCount;
        }
        finally
        {
            // Some comparison frameworks retain their disposable list after Unload throws. The
            // observation above records that; cleanup must not abort the remainder of the corpus.
            _ = ExceptionName(content.Dispose);
        }
    }

    private static string DisposeThrowPoisons()
    {
        SequencedDisposableReader.Reset();
        byte[] asset = SequencedDisposableAsset();
        var content = new MemoryContentManager(() => new TrackingStream(asset));
        content.Load<CorpusDisposable>("fixture");
        string dispose = ExceptionName(content.Dispose);
        string reload = ExceptionName(() => content.Load<CorpusDisposable>("fixture"));
        return $"{dispose}/{reload}";
    }

    private static byte[] SequencedDisposableAsset() => Container(Payload(
        typeof(SequencedDisposableReader).AssemblyQualifiedName!, 0, 1,
        writer =>
        {
            Write7Bit(writer, 1);
            Write7Bit(writer, 1);
        }));

    private static byte[] IntPayload(int serializedVersion = 1) => Payload(
        typeof(CorpusIntReader).AssemblyQualifiedName!, serializedVersion, 0,
        writer =>
        {
            Write7Bit(writer, 1);
            writer.Write(42);
        });

    private static byte[] ReaderCountZeroPayload()
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            Write7Bit(writer, 0);
            Write7Bit(writer, 0);
            Write7Bit(writer, 1);
        }

        return payload.ToArray();
    }

    private static byte[] ReaderCountTooLargePayload()
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            Write7Bit(writer, 4097);
        }

        return payload.ToArray();
    }

    private static byte[] Payload(
        string readerName,
        int readerVersion,
        int sharedResourceCount,
        Action<BinaryWriter> writeBody)
    {
        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            Write7Bit(writer, 1);
            writer.Write(readerName);
            writer.Write(readerVersion);
            Write7Bit(writer, sharedResourceCount);
            writeBody(writer);
        }

        return payload.ToArray();
    }

    private static byte[] Container(byte[] payload, byte flags = 0)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((byte)'X');
            writer.Write((byte)'N');
            writer.Write((byte)'B');
            writer.Write((byte)'w');
            writer.Write((byte)5);
            writer.Write(flags);
            writer.Write(10 + payload.Length);
            writer.Write(payload);
        }

        return stream.ToArray();
    }

    private static byte[] Mutate(byte[] source, int index, byte value)
    {
        byte[] copy = (byte[])source.Clone();
        copy[index] = value;
        return copy;
    }

    private static byte[] WithDeclaredSize(byte[] source, int size)
    {
        byte[] copy = (byte[])source.Clone();
        byte[] encoded = BitConverter.GetBytes(size);
        Array.Copy(encoded, 0, copy, 6, encoded.Length);
        return copy;
    }

    private static void Write7Bit(BinaryWriter writer, int value)
    {
        uint remaining = unchecked((uint)value);
        while (remaining >= 0x80)
        {
            writer.Write((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        writer.Write((byte)remaining);
    }

    private static void Observe(List<string> observations, string name, Func<int> action)
    {
        try
        {
            observations.Add($"{name}=ok:{action().ToString(CultureInfo.InvariantCulture)}");
        }
        catch (Exception exception)
        {
            string value = exception.GetType().Name;
            if (exception.InnerException is not null)
            {
                value += $"(inner={exception.InnerException.GetType().Name})";
            }

            observations.Add($"{name}={value}");
        }
    }

    private static string ExceptionName(Action action)
    {
        try
        {
            action();
            return "ok";
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }

    private static int Flag(bool value) => value ? 1 : 0;

    private sealed class MemoryContentManager(Func<Stream> streamFactory)
        : ContentManager(new NullServiceProvider())
    {
        protected override Stream OpenStream(string assetName) => streamFactory();
    }

    private sealed class MappedContentManager(IReadOnlyDictionary<string, byte[]> assets)
        : ContentManager(new NullServiceProvider())
    {
        protected override Stream OpenStream(string assetName) =>
            new TrackingStream(assets[assetName.Replace('\\', '/')]);
    }

    private sealed class TrackingStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}

public sealed class CorpusIntReader : ContentTypeReader<int>
{
    public override int TypeVersion => 1;

    protected override int Read(ContentReader input, int existingInstance) => input.ReadInt32();
}

public class CorpusDisposable : IDisposable
{
    public static int DisposeCount { get; private set; }

    public static void Reset() => DisposeCount = 0;

    public virtual void Dispose()
    {
        DisposeCount++;
    }
}

public sealed class SharedReferenceReader : ContentTypeReader<CorpusDisposable>
{
    protected override CorpusDisposable Read(ContentReader input, CorpusDisposable existingInstance)
    {
        var value = new CorpusDisposable();
        input.ReadSharedResource<CorpusDisposable>(_ => { });
        return value;
    }
}

public sealed class ThrowingReader : ContentTypeReader<int>
{
    protected override int Read(ContentReader input, int existingInstance) =>
        throw new InvalidOperationException("reader");
}

public sealed class ThrowingConstructorReader : ContentTypeReader<int>
{
    public ThrowingConstructorReader() => throw new InvalidOperationException("constructor");

    protected override int Read(ContentReader input, int existingInstance) => 0;
}

public sealed class ExternalReferenceReader : ContentTypeReader<int>
{
    protected override int Read(ContentReader input, int existingInstance) =>
        input.ReadExternalReference<int>();
}

public sealed class NewDisposableReader : ContentTypeReader<CorpusDisposable>
{
    protected override CorpusDisposable Read(ContentReader input, CorpusDisposable existingInstance) => new();
}

public sealed class SingletonDisposableReader : ContentTypeReader<CorpusDisposable>
{
    private static CorpusDisposable _instance = new();

    public static void Reset() => _instance = new CorpusDisposable();

    protected override CorpusDisposable Read(ContentReader input, CorpusDisposable existingInstance) => _instance;
}

public sealed class SequencedDisposableReader : ContentTypeReader<CorpusDisposable>
{
    private static int _readCount;
    private static CountingDisposable? _first;
    private static CountingDisposable? _second;

    public static int FirstDisposeCount => _first?.Count ?? 0;

    public static int SecondDisposeCount => _second?.Count ?? 0;

    public static void Reset()
    {
        _readCount = 0;
        _first = null;
        _second = null;
    }

    protected override CorpusDisposable Read(ContentReader input, CorpusDisposable existingInstance)
    {
        if (_readCount++ == 0)
        {
            return _first = new CountingDisposable(throws: true);
        }

        return _second = new CountingDisposable(throws: false);
    }

    private sealed class CountingDisposable(bool throws) : CorpusDisposable
    {
        public int Count { get; private set; }

        public override void Dispose()
        {
            Count++;
            if (throws)
            {
                throw new InvalidOperationException("dispose");
            }
        }
    }
}
