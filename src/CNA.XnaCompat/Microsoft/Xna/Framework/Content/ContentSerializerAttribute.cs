namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible <c>ContentSerializerAttribute</c>. Declared here rather than
/// subclassing <see cref="CNA.Content.ContentSerializerAttribute"/>: an attribute is looked up by
/// its exact type, so XNA source annotated <c>[ContentSerializer]</c> must find *this* type for
/// reflection over it to behave as it does in XNA.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ContentSerializerAttribute : Attribute
{
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
