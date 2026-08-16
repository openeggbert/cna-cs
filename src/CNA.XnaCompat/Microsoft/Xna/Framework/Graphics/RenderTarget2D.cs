namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTarget2D</c>. Inherits from this namespace's own <c>Texture2D</c>
/// (not <c>CNA.Graphics.RenderTarget2D</c>) so <c>Texture2D t = someRenderTarget;</c> compiles in
/// game code, matching real XNA's <c>RenderTarget2D : Texture2D</c>. Creation still goes through
/// <c>CNA.Graphics.RenderTarget2D.CreateNativeHandle</c> -- see that method's doc comment for why
/// it exists and why this doesn't just subclass it directly.
/// </summary>
public class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(CNA.Graphics.RenderTarget2D.CreateNativeHandle(graphicsDevice, width, height))
    {
    }
}
