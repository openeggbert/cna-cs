namespace CNA.Content;

/// <summary>
/// Matches real XNA's <c>ResourceContentManager</c>: a <see cref="ContentManager"/> that loads
/// assets embedded in a .NET <see cref="System.Resources.ResourceManager"/> rather than from
/// files on disk.
///
/// Implemented managed-side rather than bound, because that is what it is: the C API has no
/// resource-manager concept at all (it knows only files), and .NET resource lookup is entirely a
/// BCL concern -- design invariant #7. Asset bytes are pulled from the resource manager and the
/// base class's own loading path takes over from there.
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
