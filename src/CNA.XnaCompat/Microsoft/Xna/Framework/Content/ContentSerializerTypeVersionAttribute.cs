namespace Microsoft.Xna.Framework.Content;

/// <summary>Specifies the runtime serialization version of a type.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ContentSerializerTypeVersionAttribute : Attribute
{
    public ContentSerializerTypeVersionAttribute(int typeVersion)
    {
        TypeVersion = typeVersion;
    }

    public int TypeVersion { get; }
}
