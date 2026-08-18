namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible <c>ResourceContentManager</c>. A pure subclass of
/// <see cref="CNA.Content.ResourceContentManager"/>; the resource lookup it adds involves only BCL
/// types, so nothing needs re-typing.</summary>
public class ResourceContentManager : CNA.Content.ResourceContentManager
{
    protected internal ResourceContentManager(nint nativeHandleValue, System.Resources.ResourceManager resourceManager)
        : base(nativeHandleValue, resourceManager)
    {
    }
}
