namespace CNA.Content;

/// <summary>
/// Matches real XNA's <c>ResourceContentManager</c>: a <see cref="ContentManager"/> that loads
/// assets embedded in a .NET <see cref="System.Resources.ResourceManager"/> rather than from
/// files on disk.
///
/// Implemented managed-side rather than bound, and deliberately so -- but not for the reason this
/// comment used to give. It claimed "the C API has no resource-manager concept at all (it knows
/// only files)". A header audit found <c>cna_content_manager_create_resource</c>
/// (<c>content.h:130</c>), documented as mapping exactly this type.
///
/// The route is still not used, because the same header says what it does: "the canonical
/// embedded-resource stream is a declared placeholder in CNA, so an embedded asset load fails
/// rather than returning data". Binding it would replace a working managed implementation with one
/// that fails every load. .NET resource lookup is a BCL concern anyway (design invariant #7), so
/// asset bytes come from the resource manager and the base class's loading path takes over.
/// </summary>
public class ResourceContentManager : ContentManager
{
    private readonly System.Resources.ResourceManager _resourceManager;

    protected internal ResourceContentManager(nint nativeHandleValue, System.Resources.ResourceManager resourceManager)
        : base(nativeHandleValue)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);
        _resourceManager = resourceManager;
    }

    /// <summary>Returns the asset's bytes from the resource manager. Throws
    /// <see cref="ContentLoadException"/> naming the asset when the resource is absent or is not a
    /// byte array, matching how every other miss in this class is reported.</summary>
    protected byte[] GetResourceBytes(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        object? resource = _resourceManager.GetObject(assetName);

        return resource switch
        {
            byte[] bytes => bytes,
            null => throw new ContentLoadException($"Resource '{assetName}' was not found in the resource manager."),
            _ => throw new ContentLoadException(
                $"Resource '{assetName}' is a {resource.GetType().Name}, not the byte[] a content asset must be."),
        };
    }
}
