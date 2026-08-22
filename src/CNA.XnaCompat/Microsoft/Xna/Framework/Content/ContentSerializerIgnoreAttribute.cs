namespace Microsoft.Xna.Framework.Content;

/// <summary>Excludes a public field or property from content serialization.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ContentSerializerIgnoreAttribute : Attribute
{
}
