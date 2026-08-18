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

    /// <summary>Matches real XNA's <c>Begin(SpriteSortMode, BlendState)</c>. Re-typed for the same
    /// reason the <c>SpriteEffects</c> overloads are: <see cref="SpriteSortMode"/> is a distinct
    /// enum per namespace, so an XNA-namespaced argument would not bind to the base
    /// overload.</summary>
    public void Begin(SpriteSortMode sortMode, BlendState? blendState) =>
        Begin(sortMode, blendState, null, null, null);

    /// <summary>Matches real XNA's five-argument <c>Begin</c>. The state objects subclass their
    /// <c>CNA.Graphics</c> counterparts, so only the sort mode needs converting.</summary>
    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState) =>
        base.Begin(
            (CNA.Graphics.SpriteSortMode)(int)sortMode,
            blendState, samplerState, depthStencilState, rasterizerState);

    /// <summary>Matches real XNA's six-argument <c>Begin</c>.</summary>
    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect) =>
        Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, null);

    /// <summary>Matches real XNA's seven-argument <c>Begin</c>. The compat <see cref="Effect"/>
    /// composes its <c>CNA.Graphics</c> counterpart rather than deriving from it -- see that type's
    /// doc comment -- so its inner effect is what reaches the base.</summary>
    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect,
        Matrix? transformMatrix) =>
        base.Begin(
            (CNA.Graphics.SpriteSortMode)(int)sortMode,
            blendState, samplerState, depthStencilState, rasterizerState,
            effect?.Inner,
            transformMatrix is { } matrix ? matrix : null);

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

    /// <summary><c>DrawString(SpriteFont, string, Vector2, Color)</c> is inherited unchanged
    /// (its <c>SpriteFont</c> argument upcasts, same as <c>Draw</c>'s <c>Texture2D</c>); these
    /// two overloads need the same <c>SpriteEffects</c> override treatment as <c>Draw</c>
    /// above.</summary>
    public void DrawString(
        SpriteFont spriteFont,
        string text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth) =>
        base.DrawString(spriteFont, text, position, color, rotation, origin, scale, (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void DrawString(
        SpriteFont spriteFont,
        string text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth) =>
        base.DrawString(spriteFont, text, position, color, rotation, origin, scale, (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);
}
