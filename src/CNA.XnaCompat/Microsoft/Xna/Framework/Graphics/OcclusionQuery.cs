namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>OcclusionQuery</c>. A pure subclass -- <c>Begin</c>/<c>End</c>/
/// <c>IsComplete</c>/<c>PixelCount</c>/<c>Dispose</c> involve no namespace-divergent types and are
/// inherited unchanged; only <see cref="GraphicsDevice"/> needs re-typing.</summary>
public class OcclusionQuery : CNA.Graphics.OcclusionQuery
{
    private readonly GraphicsDevice _graphicsDevice;

    public OcclusionQuery(GraphicsDevice graphicsDevice)
        : base(graphicsDevice.Framework)
    {
        _graphicsDevice = graphicsDevice;
    }

    public new GraphicsDevice GraphicsDevice => _graphicsDevice;
}
