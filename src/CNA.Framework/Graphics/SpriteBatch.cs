using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// The single-draw-call form of <c>SpriteBatch</c>. Command buffering + a batched
/// <c>cna_sprite_batch_draw_many</c> call, per ../../cnabinding/analysis_binding.md §22, is
/// Phase 5 (plan.md) -- not implemented yet.
/// </summary>
public class SpriteBatch : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public SpriteBatch(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_sprite_batch_create(new CnaHandle(graphicsDevice.NativeHandleValue), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(SpriteBatch));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_sprite_batch_release(new CnaHandle(h)));
    }

    private nint NativeHandleValue => _handle.DangerousGetHandle();

    public void Begin()
    {
        CnaResult result = Native.cna_sprite_batch_begin(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(Begin));
    }

    public void Draw(Texture2D texture, Vector2 position, Color color)
    {
        ArgumentNullException.ThrowIfNull(texture);

        CnaResult result = Native.cna_sprite_batch_draw(
            new CnaHandle(NativeHandleValue),
            new CnaHandle(texture.NativeHandleValue),
            position.ToNative(),
            color.ToNative());
        CnaException.ThrowIfFailed(result, nameof(Draw));
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color) =>
        DrawEx(texture, position, sourceRectangle, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color) =>
        Draw(texture, destinationRectangle, null, color);

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color) =>
        DrawEx(texture, destinationRectangle, sourceRectangle, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);

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
        DrawEx(texture, position, sourceRectangle, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth);

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
        DrawEx(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) =>
        DrawEx(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth);

    /// <summary>The position/rotation/scale primitive every extended <c>Draw</c> overload above
    /// funnels through -- one native call (<see cref="Native.cna_sprite_batch_draw_ex"/>) backing
    /// the whole overload family, per this project's usual "minimal native surface, C# handles
    /// convenience overloads" approach.</summary>
    private void DrawEx(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(texture);

        Rectangle source = sourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);

        var command = new CnaSpriteDrawCommand(
            new CnaHandle(texture.NativeHandleValue),
            position.ToNative(),
            source.ToNative(),
            color.ToNative(),
            rotation,
            origin.ToNative(),
            scale.ToNative(),
            (int)effects,
            layerDepth);

        CnaResult result = Native.cna_sprite_batch_draw_ex(new CnaHandle(NativeHandleValue), in command);
        CnaException.ThrowIfFailed(result, nameof(Draw));
    }

    /// <summary>The destination-rectangle overloads' primitive: XNA specifies these by the
    /// screen-space rectangle the sprite should fill rather than by position+scale, so this
    /// resolves that rectangle (and the source-vs-whole-texture size it is scaled against) down
    /// to the position+scale form <see cref="DrawEx(Texture2D,Vector2,Rectangle?,Color,float,Vector2,Vector2,SpriteEffects,float)"/>
    /// expects, then delegates to it -- the actual native call happens there, not here.</summary>
    private void DrawEx(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(texture);

        int sourceWidth = sourceRectangle?.Width ?? texture.Width;
        int sourceHeight = sourceRectangle?.Height ?? texture.Height;
        var scale = new Vector2(
            sourceWidth == 0 ? 0f : destinationRectangle.Width / (float)sourceWidth,
            sourceHeight == 0 ? 0f : destinationRectangle.Height / (float)sourceHeight);

        DrawEx(
            texture,
            new Vector2(destinationRectangle.X, destinationRectangle.Y),
            sourceRectangle,
            color,
            rotation,
            origin,
            scale,
            effects,
            layerDepth);
    }

    public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color) =>
        DrawString(spriteFont, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

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
        DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth);

    /// <summary>Draws each glyph as its own <c>Draw</c> call against <see cref="SpriteFont.Texture"/>,
    /// using the glyph placements <see cref="SpriteFont.AppendGlyphPlacements"/> computes -- there
    /// is no dedicated native draw-string call (see <see cref="SpriteFont"/>'s doc comment for why
    /// none was needed). Offsetting each glyph's own <c>origin</c> by its placement anchor, rather
    /// than pre-transforming each glyph's <paramref name="position"/>, is what makes the whole
    /// string rotate/scale as one rigid body around <paramref name="origin"/>/<paramref name="position"/>
    /// -- the same trick <c>Draw</c>'s own origin already performs for a single sprite, just
    /// applied once per glyph.</summary>
    public void DrawString(
        SpriteFont spriteFont,
        string text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        ArgumentNullException.ThrowIfNull(spriteFont);
        ArgumentNullException.ThrowIfNull(text);

        var placements = new List<SpriteFont.GlyphPlacement>();
        spriteFont.AppendGlyphPlacements(text, placements);

        foreach (SpriteFont.GlyphPlacement placement in placements)
        {
            DrawEx(
                spriteFont.Texture,
                position,
                placement.SourceRectangle,
                color,
                rotation,
                origin - placement.Anchor,
                scale,
                effects,
                layerDepth);
        }
    }

    public void End()
    {
        CnaResult result = Native.cna_sprite_batch_end(new CnaHandle(NativeHandleValue));
        CnaException.ThrowIfFailed(result, nameof(End));
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
