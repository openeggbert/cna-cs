namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture</c>. A pure subclass -- <c>LevelCount</c>/<c>Dispose</c>
/// are inherited unchanged from <see cref="CNA.Graphics.Texture"/>; only <see cref="Format"/>
/// needs a `new` override, since <see cref="SurfaceFormat"/> is this namespace's own enum type
/// (see that enum's own doc comment for why the two namespaces duplicate rather than alias).</summary>
public abstract class Texture : CNA.Graphics.Texture
{
    protected Texture(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;

    public new SurfaceFormat Format => (SurfaceFormat)(int)base.Format;
}
