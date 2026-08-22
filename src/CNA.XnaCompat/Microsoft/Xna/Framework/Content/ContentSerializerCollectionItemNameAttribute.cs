namespace Microsoft.Xna.Framework.Content;

/// <summary>Specifies the XML element name used for members of a collection.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ContentSerializerCollectionItemNameAttribute : Attribute
{
    public ContentSerializerCollectionItemNameAttribute(string collectionItemName)
    {
        ArgumentNullException.ThrowIfNull(collectionItemName);
        CollectionItemName = collectionItemName;
    }

    public string CollectionItemName { get; }
}
