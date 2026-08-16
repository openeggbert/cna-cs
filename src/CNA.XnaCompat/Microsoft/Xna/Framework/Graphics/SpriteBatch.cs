namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>SpriteBatch</c>. <c>Begin()</c>/<c>Draw(Texture2D, Vector2, Color)</c>/
/// <c>End()</c>/<c>Dispose()</c> are inherited unchanged from
/// <see cref="CNA.Graphics.SpriteBatch"/> -- the <c>Texture2D</c> argument upcasts and
/// the <c>Vector2</c>/<c>Color</c> arguments convert through their implicit operators, so no
/// override is needed here. Additional XNA <c>Draw</c> overloads (source rectangle, rotation,
/// scale, <c>SpriteEffects</c>, layer depth) are Phase 4 (plan.md).
/// </summary>
public class SpriteBatch : CNA.Graphics.SpriteBatch
{
    public SpriteBatch(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }
}
