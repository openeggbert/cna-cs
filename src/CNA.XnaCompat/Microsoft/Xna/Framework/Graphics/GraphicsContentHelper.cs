namespace Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Content;

/// <summary>
/// The graphics device a content reader builds its resources on.
///
/// XNA has an internal helper of this exact name and shape, and this is the same thing: the device
/// is not on the reader, it is a service the content manager's provider hands out. A reader that
/// needs one -- every texture, buffer or effect reader -- goes through here so they all fail the
/// same way when the manager was constructed without one, which is a real and easy mistake to make
/// from a unit test.
/// </summary>
internal static class GraphicsContentHelper
{
    internal static GraphicsDevice GraphicsDeviceFromContentReader(ContentReader input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.ContentManager.ServiceProvider?.GetService(typeof(IGraphicsDeviceService))
            is IGraphicsDeviceService { GraphicsDevice: { } device })
        {
            return device;
        }

        throw new ContentLoadException(
            $"Content asset '{input.AssetName}' contains a graphics resource, but its ContentManager's service " +
            "provider supplies no IGraphicsDeviceService with a GraphicsDevice.");
    }
}
