namespace Microsoft.Xna.Framework.Content;

/// <summary>Base class for a reader of one managed content type.</summary>
public abstract class ContentTypeReader
{
    private readonly Type _targetType;

    /// <summary>Initializes a reader for <paramref name="targetType"/>.</summary>
    protected ContentTypeReader(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        _targetType = targetType;
    }

    /// <summary>Gets the managed type this reader produces.</summary>
    public Type TargetType => _targetType;

    /// <summary>Gets the format version understood by this reader.</summary>
    public virtual int TypeVersion => 0;

    /// <summary>Whether this reader can populate an existing instance.</summary>
    public virtual bool CanDeserializeIntoExistingObject => false;

    /// <summary>Initializes the reader after its containing XNB table has been built.</summary>
    protected internal virtual void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
    }

    /// <summary>Reads one object, optionally into <paramref name="existingInstance"/>.</summary>
    protected internal abstract object Read(ContentReader input, object? existingInstance);
}

/// <summary>Strongly typed base class for a custom content reader.</summary>
public abstract class ContentTypeReader<T> : ContentTypeReader
{
    /// <summary>Initializes a reader for <typeparamref name="T"/>.</summary>
    protected ContentTypeReader()
        : base(typeof(T))
    {
    }

    protected internal override object Read(ContentReader input, object? existingInstance) =>
        Read(input, existingInstance is null ? default! : (T)existingInstance)!;

    /// <summary>Reads one <typeparamref name="T"/> value, optionally reusing an existing instance.</summary>
    protected internal abstract T Read(ContentReader input, T existingInstance);
}
