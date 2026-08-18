namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible <c>ContentTypeReaderManager</c>. A forwarding facade -- static
/// classes cannot be subclassed, the same reason
/// <see cref="Microsoft.Xna.Framework.Input.Keyboard"/> forwards.</summary>
public static class ContentTypeReaderManager
{
    public static bool IsRegistered(string canonicalName) => CNA.Content.ContentTypeReaderManager.IsRegistered(canonicalName);

    public static ContentTypeReader CreateReader(string canonicalName) =>
        new(CNA.Content.ContentTypeReaderManager.CreateReader(canonicalName));

    public static void ClearTypeCreators() => CNA.Content.ContentTypeReaderManager.ClearTypeCreators();
}
