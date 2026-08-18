namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture3D</c>. A pure subclass -- <c>Width</c>/<c>Height</c>/
/// <c>Depth</c>/<c>LevelCount</c>/<c>Dispose</c> are all inherited unchanged from
/// <see cref="CNA.Graphics.Texture3D"/>; only the <see cref="SurfaceFormat"/>-typed members need
/// re-typing, since that enum is duplicated per namespace (see its own doc comment).</summary>
public class Texture3D : CNA.Graphics.Texture3D
{
    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : base(graphicsDevice, width, height, depth)
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, width, height, depth, mipMap, (CNA.Graphics.SurfaceFormat)(int)format)
    {
    }

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;

    public new SurfaceFormat Format => (SurfaceFormat)(int)base.Format;
}
