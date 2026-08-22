using System.Resources;

namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible resource-backed content manager.</summary>
public class ResourceContentManager : ContentManager
{
    private readonly ResourceManager _resourceManager;

    public ResourceContentManager(IServiceProvider serviceProvider, ResourceManager resourceManager)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);
        _resourceManager = resourceManager;
    }

    protected override Stream OpenStream(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        object? resource = _resourceManager.GetObject(assetName);
        return resource switch
        {
            byte[] bytes => new MemoryStream(bytes, writable: false),
            null => throw new ContentLoadException($"Resource '{assetName}' was not found."),
            _ => throw new ContentLoadException(
                $"Resource '{assetName}' is not stored in binary format."),
        };
    }
}
