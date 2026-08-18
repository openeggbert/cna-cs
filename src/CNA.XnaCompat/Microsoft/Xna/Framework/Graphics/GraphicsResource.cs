namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>GraphicsResource</c>. A pure subclass -- every member is
/// inherited unchanged from <see cref="CNA.Graphics.GraphicsResource"/> except the
/// <see cref="GraphicsDevice"/> property, which needs a `new` override so it reports this
/// namespace's device type (the same downcast pass-through pattern
/// <see cref="GraphicsDevice.Indices"/> uses, and holding no field of its own for the same
/// reason).</summary>
public abstract class GraphicsResource : CNA.Graphics.GraphicsResource
{
    protected GraphicsResource(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;
}
