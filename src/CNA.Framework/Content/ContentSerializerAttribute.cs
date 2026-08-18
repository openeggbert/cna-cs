namespace CNA.Content;

/// <summary>
/// Matches real XNA's <c>ContentSerializerAttribute</c>: marks how a field or property is written
/// to and read from a content file.
///
/// Purely declarative, with no native counterpart by design -- in XNA it is read by the *content
/// pipeline* (a build-time assembly this project does not implement, and which the scope mandate
/// explicitly excludes) and by reflection-driven readers. It exists here so game types annotated
/// for XNA compile unchanged, and so a hand-written <see cref="ContentTypeReader"/> can read the
/// same metadata if it wants.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ContentSerializerAttribute : Attribute
{
    /// <summary>The element name used in the content file. Defaults to the member's own name when
    /// unset, which is what XNA does.</summary>
    public string? ElementName { get; set; }

    public bool AllowNull { get; set; } = true;

    public string? CollectionItemName { get; set; }

    public bool FlattenContent { get; set; }

    public bool HasCollectionItemName => CollectionItemName is not null;

    public bool Optional { get; set; }

    public bool SharedResource { get; set; }

    public ContentSerializerAttribute Clone() => new()
    {
        ElementName = ElementName,
        AllowNull = AllowNull,
        CollectionItemName = CollectionItemName,
        FlattenContent = FlattenContent,
        Optional = Optional,
        SharedResource = SharedResource,
    };
}
