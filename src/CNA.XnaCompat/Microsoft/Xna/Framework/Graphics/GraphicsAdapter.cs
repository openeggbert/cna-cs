namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>GraphicsAdapter</c>. A pure subclass -- the scalar properties
/// are inherited unchanged; only the members whose types differ per namespace need re-typing. See
/// <see cref="CNA.Graphics.GraphicsAdapter"/> for why the enumeration entry points take a device
/// rather than being static as in real XNA.</summary>
public class GraphicsAdapter : CNA.Graphics.GraphicsAdapter
{
    internal GraphicsAdapter(GraphicsDevice graphicsDevice, uint adapterIndex)
        : base(graphicsDevice, adapterIndex)
    {
    }

    public new DisplayMode CurrentDisplayMode => DisplayMode.FromFramework(base.CurrentDisplayMode);

    public new DisplayModeCollection SupportedDisplayModes => DisplayModeCollection.FromFramework(base.SupportedDisplayModes);

    public bool IsProfileSupported(GraphicsProfile profile) =>
        base.IsProfileSupported((CNA.Graphics.GraphicsProfile)(int)profile);
}
