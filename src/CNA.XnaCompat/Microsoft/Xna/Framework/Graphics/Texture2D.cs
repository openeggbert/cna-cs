namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture2D</c>. <c>Width</c>/<c>Height</c>/<c>SetData</c>/
/// <c>Dispose</c> are inherited unchanged from <see cref="CNA.Framework.Graphics.Texture2D"/>.</summary>
public class Texture2D : CNA.Framework.Graphics.Texture2D
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, width, height)
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    protected internal Texture2D(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }
}
