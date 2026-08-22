using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0 SpriteBatch facade. Its public base is GraphicsResource; the CNA batch remains
/// a private implementation object and owns the one native batch handle.</summary>
public class SpriteBatch : GraphicsResource
{
    private readonly CNA.Graphics.SpriteBatch _inner;

    public SpriteBatch(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        _inner = new CNA.Graphics.SpriteBatch(graphicsDevice.Framework);
    }

    public void Begin() => _inner.Begin();

    public void Begin(SpriteSortMode sortMode, BlendState? blendState) =>
        _inner.Begin((CNA.Graphics.SpriteSortMode)(int)sortMode, blendState?.Framework);

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState) =>
        _inner.Begin(
            (CNA.Graphics.SpriteSortMode)(int)sortMode,
            blendState?.Framework,
            samplerState?.Framework,
            depthStencilState?.Framework,
            rasterizerState?.Framework);

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect) =>
        _inner.Begin(
            (CNA.Graphics.SpriteSortMode)(int)sortMode,
            blendState?.Framework,
            samplerState?.Framework,
            depthStencilState?.Framework,
            rasterizerState?.Framework,
            effect?.Inner);

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect,
        Matrix transformMatrix) =>
        _inner.Begin(
            (CNA.Graphics.SpriteSortMode)(int)sortMode,
            blendState?.Framework,
            samplerState?.Framework,
            depthStencilState?.Framework,
            rasterizerState?.Framework,
            effect?.Inner,
            transformMatrix);

    public void End() => _inner.End();

    public void Draw(Texture2D texture, Vector2 position, Color color) =>
        _inner.Draw(Backend(texture), position, color);

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color) =>
        _inner.Draw(Backend(texture), position, sourceRectangle, color);

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
        _inner.Draw(
            Backend(texture), position, sourceRectangle, color, rotation, origin, scale,
            (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

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
        _inner.Draw(
            Backend(texture), position, sourceRectangle, color, rotation, origin, scale,
            (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color) =>
        _inner.Draw(Backend(texture), destinationRectangle, color);

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color) =>
        _inner.Draw(Backend(texture), destinationRectangle, sourceRectangle, color);

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) =>
        _inner.Draw(
            Backend(texture), destinationRectangle, sourceRectangle, color, rotation, origin,
            (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color) =>
        _inner.DrawString(spriteFont, text, position, color);

    public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
    {
        ArgumentNullException.ThrowIfNull(text);
        _inner.DrawString(spriteFont, text.ToString(), position, color);
    }

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
        _inner.DrawString(
            spriteFont, text, position, color, rotation, origin, scale,
            (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void DrawString(
        SpriteFont spriteFont,
        StringBuilder text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(text);
        DrawString(spriteFont, text.ToString(), position, color, rotation, origin, scale, effects, layerDepth);
    }

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
        _inner.DrawString(
            spriteFont, text, position, color, rotation, origin, scale,
            (CNA.Graphics.SpriteEffects)(int)effects, layerDepth);

    public void DrawString(
        SpriteFont spriteFont,
        StringBuilder text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(text);
        DrawString(spriteFont, text.ToString(), position, color, rotation, origin, scale, effects, layerDepth);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        _inner.Dispose();
        base.Dispose(disposing);
    }

    private static CNA.Graphics.Texture2D Backend(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        return (CNA.Graphics.Texture2D)texture.FrameworkTexture;
    }
}
