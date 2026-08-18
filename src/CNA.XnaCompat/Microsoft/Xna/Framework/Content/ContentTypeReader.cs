namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// XNA 4.0-compatible <c>ContentTypeReader</c>.
///
/// A thin re-typing wrapper rather than a subclass: <see cref="CNA.Content.ContentTypeReader"/>
/// wraps a native handle it obtained from the registry, and its only constructor takes that raw
/// handle -- which CNA.XnaCompat must never name (design invariant #5). Wrapping the finished
/// object keeps the handle entirely on the CNA side.
/// </summary>
public class ContentTypeReader : IDisposable
{
    private readonly CNA.Content.ContentTypeReader _reader;

    internal ContentTypeReader(CNA.Content.ContentTypeReader reader)
    {
        _reader = reader;
    }

    public string TargetTypeName => _reader.TargetTypeName;

    public int TypeVersion => _reader.TypeVersion;

    public bool CanDeserializeIntoExistingObject => _reader.CanDeserializeIntoExistingObject;

    public bool SupportsVersion(int serializedVersion) => _reader.SupportsVersion(serializedVersion);

    public void Initialize() => _reader.Initialize();

    public bool ReadUntyped(ContentReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return _reader.ReadUntyped(reader.Framework);
    }

    public void Dispose()
    {
        _reader.Dispose();
        GC.SuppressFinalize(this);
    }
}
