namespace Microsoft.Xna.Framework.Content;

/// <summary>Specifies the runtime type name emitted for an intermediate content value.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ContentSerializerRuntimeTypeAttribute : Attribute
{
    public ContentSerializerRuntimeTypeAttribute(string runtimeType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);
        RuntimeType = runtimeType;
    }

    public string RuntimeType { get; }
}
