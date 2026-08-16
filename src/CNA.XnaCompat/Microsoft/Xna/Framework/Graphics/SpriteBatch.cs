namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>SpriteBatch</c>. <c>Begin()</c>/<c>End()</c>/<c>Dispose()</c> and every
/// <c>Draw</c> overload *without* a <c>SpriteEffects</c> parameter are inherited unchanged from
/// <see cref="CNA.Graphics.SpriteBatch"/> -- the <c>Texture2D</c> argument upcasts and the
/// <c>Vector2</c>/<c>Rectangle?</c>/<c>Color</c> arguments convert through their implicit
/// operators (nullable <c>Rectangle?</c> included -- C# lifts a value type's user-defined
/// conversion operator over <c>Nullable&lt;T&gt;</c> automatically), so no override is needed for
/// those. The three overloads that take <c>SpriteEffects</c> *do* need an override below: it is a
/// distinct enum type from <see cref="CNA.Graphics.SpriteEffects"/> (enums cannot define
/// conversion operators in C#), so a plain XNA-namespaced <c>SpriteEffects.None</c> argument would
/// not otherwise bind to the base overload.
/// </summary>
public class SpriteBatch : CNA.Graphics.SpriteBatch
{
    public SpriteBatch(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth) =>
        base.Draw(texture, position, sourceRectangle, color, rotation, origin, scale, (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth) =>
        base.Draw(texture, position, sourceRectangle, color, rotation, origin, scale, (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) =>
        base.Draw(texture, destinationRectangle, sourceRectangle, color, rotation, origin, (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);
}
