namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// The bodies of XNA's generic built-in readers, transcribed from the decompiled 4.0 readers of the
/// same names.
///
/// The shared subtlety in all of them is which route an element takes.
/// <c>ReadObject&lt;T&gt;(elementReader)</c> reads a value type inline with the reader it already
/// holds and a reference type through the polymorphic route, with that object's own type-index
/// prefix. So <c>List&lt;int&gt;</c> and <c>List&lt;string&gt;</c> have different per-element
/// layouts, and choosing the wrong one does not fail where the mistake is -- it misreads every
/// remaining byte of the asset. Each reader below therefore goes through the overload that
/// branches, never <c>ReadRawObject</c>, except where XNA itself uses the raw route.
/// </summary>
internal sealed class ListReader<T> : ContentTypeReader<List<T>>
{
    private ContentTypeReader? _elementReader;

    public override bool CanDeserializeIntoExistingObject => true;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _elementReader = manager.GetTypeReader(typeof(T)) ?? BuiltinReaders.TryCreateForTargetType(typeof(T));
    }

    protected internal override List<T> Read(ContentReader input, List<T> existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);
        ContentTypeReader elementReader = ContentReaderElements.Require<T>(_elementReader, input);

        int count = input.ReadInt32();
        List<T> list = existingInstance ?? new List<T>(ContentReaderElements.Capacity(count, input));
        for (int index = 0; index < count; index++)
        {
            list.Add(input.ReadObject<T>(elementReader));
        }

        return list;
    }
}

/// <summary>See <see cref="ListReader{T}"/>.</summary>
internal sealed class ArrayReader<T> : ContentTypeReader<T[]>
{
    private ContentTypeReader? _elementReader;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _elementReader = manager.GetTypeReader(typeof(T)) ?? BuiltinReaders.TryCreateForTargetType(typeof(T));
    }

    protected internal override T[] Read(ContentReader input, T[] existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);
        ContentTypeReader elementReader = ContentReaderElements.Require<T>(_elementReader, input);

        int count = input.ReadInt32();
        _ = ContentReaderElements.Capacity(count, input);
        var items = new T[count];
        for (int index = 0; index < count; index++)
        {
            items[index] = input.ReadObject<T>(elementReader);
        }

        return items;
    }
}

/// <summary>See <see cref="ListReader{T}"/>.</summary>
internal sealed class DictionaryReader<TKey, TValue> : ContentTypeReader<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    private ContentTypeReader? _keyReader;
    private ContentTypeReader? _valueReader;

    public override bool CanDeserializeIntoExistingObject => true;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _keyReader = manager.GetTypeReader(typeof(TKey)) ?? BuiltinReaders.TryCreateForTargetType(typeof(TKey));
        _valueReader = manager.GetTypeReader(typeof(TValue)) ?? BuiltinReaders.TryCreateForTargetType(typeof(TValue));
    }

    protected internal override Dictionary<TKey, TValue> Read(
        ContentReader input, Dictionary<TKey, TValue> existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);
        ContentTypeReader keyReader = ContentReaderElements.Require<TKey>(_keyReader, input);
        ContentTypeReader valueReader = ContentReaderElements.Require<TValue>(_valueReader, input);

        int count = input.ReadInt32();
        Dictionary<TKey, TValue> items =
            existingInstance ?? new Dictionary<TKey, TValue>(ContentReaderElements.Capacity(count, input));
        for (int index = 0; index < count; index++)
        {
            TKey key = input.ReadObject<TKey>(keyReader);
            TValue value = input.ReadObject<TValue>(valueReader);
            items.Add(key, value);
        }

        return items;
    }
}

/// <summary>
/// See <see cref="ListReader{T}"/>. This one uses the raw route rather than the branching one,
/// because XNA's <c>NullableReader</c> is constrained to <c>struct</c> and calls
/// <c>ReadRawObject</c> -- the underlying value is never tagged.
/// </summary>
internal sealed class NullableReader<T> : ContentTypeReader<T?>
    where T : struct
{
    private ContentTypeReader? _underlyingReader;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _underlyingReader = manager.GetTypeReader(typeof(T)) ?? BuiltinReaders.TryCreateForTargetType(typeof(T));
    }

    protected internal override T? Read(ContentReader input, T? existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.ReadBoolean()
            ? input.ReadRawObject<T>(ContentReaderElements.Require<T>(_underlyingReader, input))
            : null;
    }
}

/// <summary>
/// An enum reads as its underlying integral type, which the file does not record -- XNA takes it
/// from the CLR type, and so does this, because the reader is closed over the real enum type.
/// </summary>
internal sealed class EnumReader<T> : ContentTypeReader<T>
    where T : struct, Enum
{
    private ContentTypeReader? _underlyingReader;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        Type underlying = Enum.GetUnderlyingType(typeof(T));
        _underlyingReader = manager.GetTypeReader(underlying) ?? BuiltinReaders.TryCreateForTargetType(underlying);
    }

    protected internal override T Read(ContentReader input, T existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);
        object raw = input.ReadRawObject<object>(ContentReaderElements.Require<T>(_underlyingReader, input));
        return (T)Enum.ToObject(typeof(T), raw);
    }
}

/// <summary>Shared checks for the collection readers, kept in one place so their failures read the
/// same.</summary>
internal static class ContentReaderElements
{
    internal static ContentTypeReader Require<T>(ContentTypeReader? reader, ContentReader input) =>
        reader ?? throw new ContentLoadException(
            $"Content asset '{input.AssetName}' needs a content type reader for {typeof(T)} and its " +
            "type-reader table does not declare one.");

    /// <summary>
    /// Bounds a count read from the file before it is used as a capacity.
    ///
    /// A corrupt or misaligned asset reads a plausible-looking integer here, and the first thing
    /// that happens to it is an allocation. Reporting the file as corrupt beats an
    /// <c>OutOfMemoryException</c> from somewhere with no asset name in it.
    /// </summary>
    internal static int Capacity(int count, ContentReader input)
    {
        if (count is < 0 or > 10_000_000)
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' declares an implausible collection length {count}.");
        }

        return Math.Min(count, 1024);
    }
}
