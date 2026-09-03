using System.Runtime.InteropServices;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Every <c>Draw</c>/<c>DrawString</c> call buffers a <see cref="CnaSpriteDrawCommand"/> in
/// managed code instead of calling native immediately; <see cref="End"/> flushes the whole batch
/// through one <c>cna_sprite_batch_submit_scaled_many</c> call -- the real, confirmed ABI's own
/// batched-submission route for position+scale draws (<c>CNA.Interop.Native</c>'s SpriteBatch
/// section explains why this is the one this project needs, not the destination-rectangle-based
/// <c>cna_sprite_batch_submit_many</c>). This also introduced real <c>Begin</c>/<c>End</c> pairing
/// validation this type never had before
/// (there was nothing to validate when every <c>Draw</c> call went straight to native) --
/// <c>Draw</c>-before-<see cref="Begin()"/>, <see cref="End"/>-before-<see cref="Begin()"/>, and
/// calling <see cref="Begin()"/> twice without an intervening <see cref="End"/> all now throw
/// <see cref="InvalidOperationException"/>, matching real XNA/MonoGame's own behavior there
/// using the same validation order as XNA's implementation.
///
/// Despite all of the above being pure managed logic, still not independently testable: unlike
/// <see cref="GraphicsDevice"/>/<see cref="Texture2D"/>, <see cref="SpriteBatch"/> has no
/// raw-handle-wrapping constructor to construct a test instance without a real native call --
/// it never needed one for any production reason (nothing wraps an already-created
/// <c>SpriteBatch</c> handle the way <c>ContentManager</c> wraps an already-created
/// <c>Texture2D</c> one), so adding one purely to unlock testing this new logic in isolation
/// would be a test-only constructor with no other justification, unlike the two existing
/// precedents. Noted rather than silently left untested.
/// </summary>
public class SpriteBatch : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly List<SpriteFont.GlyphPlacement> _glyphPlacementBuffer = [];
    private readonly List<CnaSpriteDrawCommand> _commandBuffer = [];

    /// <summary>
    /// Every texture referenced by a buffered command, held until the batch is flushed.
    ///
    /// <see cref="CnaSpriteDrawCommand"/> stores only a raw handle, so between a
    /// <c>Draw</c> and the <see cref="End"/> that flushes it nothing else keeps the managed
    /// <see cref="Texture"/> reachable -- and an unreachable texture may have its
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> finalizer run
    /// <c>cna_texture2d_destroy</c> in between, leaving <see cref="End"/> to draw a destroyed
    /// texture. Unlike the rest of WP17 this window spans a whole frame rather than a single call,
    /// so a <see cref="GC.KeepAlive(object)"/> cannot express it; the batch has to hold the
    /// references itself.
    ///
    /// Reference equality rather than the default comparer: two distinct textures must both be
    /// held even if some future <c>Equals</c> considers them equal, and the common case (many
    /// draws of one texture) collapses to a single entry.
    /// </summary>
    private readonly HashSet<Texture> _referencedTextures = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Strings queued for the native text route, each remembering how many sprites preceded it.
    ///
    /// That count is the whole point. Sprites and strings must reach the renderer in the order the
    /// game issued them, and they leave through two different native routes, so the flush replays
    /// them interleaved: the sprites before a string, then the string, then the next run. Submitting
    /// all sprites and then all strings would be one call fewer and would draw a HUD underneath the
    /// scene it labels.
    /// </summary>
    private readonly List<PendingText> _pendingText = [];

    /// <summary>Fonts referenced this batch, kept alive for the same reason
    /// <see cref="_referencedTextures"/> keeps textures alive: native holds the handle until
    /// <c>End</c> and a collected font would destroy it early.</summary>
    private readonly HashSet<SpriteFont> _referencedFonts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Set when a renderer refuses the native text route, after which this batch draws
    /// every string the per-glyph way. One refusal is enough: the answer is a property of the
    /// renderer, not of the string.</summary>
    private bool _nativeTextRefused;

    /// <summary>The texture of the previous buffered sprite, so a run of one texture consults
    /// <see cref="_referencedTextures"/> once instead of once per sprite. Cleared wherever that
    /// set is, since a stale value would skip adding a texture the cleared set no longer holds.</summary>
    private Texture? _lastReferencedTexture;

    /// <summary>
    /// Makes this batch draw text the per-glyph way, as a renderer that refuses the native route
    /// would.
    ///
    /// That fallback is the branch that keeps adopting the native route safe, and on a renderer
    /// which accepts the route it never runs -- so without this it would ship untested, which is
    /// the same hole <c>NotifyContentLostResourcesForTesting</c> exists to close for
    /// <c>ContentLost</c>. There is no game reason to call it.
    /// </summary>
    internal void ForceGlyphQuadTextForTesting() => _nativeTextRefused = true;
    private bool _hasBegun;

    public SpriteBatch(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_sprite_batch_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(SpriteBatch));

        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_sprite_batch_destroy(new CnaHandle(h)).IsSuccess());
    }

    private nint NativeHandleValue => _handle.DangerousGetHandle();

    public void Begin()
    {
        BeginGuard();

        var beginInfo = new CnaSpriteBatchBeginInfo();
        CnaResult result = Native.cna_sprite_batch_begin(new CnaHandle(NativeHandleValue), in beginInfo);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Begin));

        BeginSucceeded();
    }

    /// <summary>Matches real XNA's <c>Begin(SpriteSortMode, BlendState)</c>.</summary>
    public void Begin(SpriteSortMode sortMode, BlendState? blendState) =>
        Begin(sortMode, blendState, null, null, null);

    /// <summary>
    /// Matches real XNA's five-argument <c>Begin</c>.
    ///
    /// A <see langword="null"/> state means "the canonical default for that slot" -- AlphaBlend,
    /// LinearClamp, DepthStencilState.None, or CullCounterClockwise. The current native converter
    /// rejects null descriptor pointers, so this layer supplies those four descriptors explicitly.
    /// </summary>
    public unsafe void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState)
    {
        BeginGuard();

        BlendState effectiveBlend = blendState ?? BlendState.AlphaBlend;
        SamplerState effectiveSampler = samplerState ?? SamplerState.LinearClamp;
        DepthStencilState effectiveDepthStencil = depthStencilState ?? DepthStencilState.None;
        RasterizerState effectiveRasterizer = rasterizerState ?? RasterizerState.CullCounterClockwise;
        CnaBlendState blend = effectiveBlend.ToNative();
        CnaSamplerState sampler = effectiveSampler.ToNative();
        CnaDepthStencilState depthStencil = effectiveDepthStencil.ToNative();
        CnaRasterizerState rasterizer = effectiveRasterizer.ToNative();

        CnaResult result = Native.cna_sprite_batch_begin_with_states(
            new CnaHandle(NativeHandleValue),
            (uint)sortMode,
            &blend,
            &sampler,
            &depthStencil,
            &rasterizer);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Begin));

        BeginSucceeded();
    }

    /// <summary>Matches real XNA's six-argument <c>Begin</c>, with a custom
    /// <see cref="Effect"/>.</summary>
    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect) =>
        Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, null);

    /// <summary>Matches real XNA's seven-argument <c>Begin</c>: a custom effect plus a transform
    /// applied to every sprite in the batch.</summary>
    public unsafe void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect,
        Matrix? transformMatrix)
    {
        BeginGuard();

        BlendState effectiveBlend = blendState ?? BlendState.AlphaBlend;
        SamplerState effectiveSampler = samplerState ?? SamplerState.LinearClamp;
        DepthStencilState effectiveDepthStencil = depthStencilState ?? DepthStencilState.None;
        RasterizerState effectiveRasterizer = rasterizerState ?? RasterizerState.CullCounterClockwise;
        CnaBlendState blend = effectiveBlend.ToNative();
        CnaSamplerState sampler = effectiveSampler.ToNative();
        CnaDepthStencilState depthStencil = effectiveDepthStencil.ToNative();
        CnaRasterizerState rasterizer = effectiveRasterizer.ToNative();
        CnaMatrix transform = transformMatrix?.ToNative() ?? default;

        CnaResult result = Native.cna_sprite_batch_begin_with_effect(
            new CnaHandle(NativeHandleValue),
            (uint)sortMode,
            &blend,
            &sampler,
            &depthStencil,
            &rasterizer,
            effect is null ? CnaHandle.Zero : new CnaHandle(effect.NativeEffectHandleValue),
            transformMatrix is null ? null : &transform);
        GC.KeepAlive(this);
        GC.KeepAlive(effect);
        CnaException.ThrowIfFailed(result, nameof(Begin));

        BeginSucceeded();
    }

    /// <summary>Shared precondition. Extracted when the four state-taking overloads landed, so
    /// "Begin without End" means the same thing on all five rather than on whichever one happened
    /// to be written first.</summary>
    private void BeginGuard()
    {
        if (_hasBegun)
        {
            throw new InvalidOperationException(
                "Begin cannot be called again until End has been successfully called.");
        }
    }

    /// <summary>Shared post-condition. Runs only after the native call succeeded, so a failed
    /// <c>Begin</c> leaves the batch closed rather than half-open.</summary>
    private void BeginSucceeded()
    {
        _commandBuffer.Clear();
        _pendingText.Clear();
        _referencedTextures.Clear();
        _lastReferencedTexture = null;
        _referencedFonts.Clear();
        _hasBegun = true;
    }

    public void Draw(Texture texture, Vector2 position, Color color) =>
        DrawEx(texture, position, null, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, nameof(Draw));

    public void Draw(Texture texture, Vector2 position, Rectangle? sourceRectangle, Color color) =>
        DrawEx(texture, position, sourceRectangle, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, nameof(Draw));

    public void Draw(Texture texture, Rectangle destinationRectangle, Color color) =>
        Draw(texture, destinationRectangle, null, color);

    public void Draw(Texture texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color) =>
        DrawEx(texture, destinationRectangle, sourceRectangle, color, 0f, Vector2.Zero, SpriteEffects.None, 0f, nameof(Draw));

    public void Draw(
        Texture texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth) =>
        DrawEx(texture, position, sourceRectangle, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth, nameof(Draw));

    public void Draw(
        Texture texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth) =>
        DrawEx(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth, nameof(Draw));

    public void Draw(
        Texture texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) =>
        DrawEx(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth, nameof(Draw));

    /// <summary>The position/rotation/scale primitive every <c>Draw</c>/<c>DrawString</c> call
    /// above funnels through -- appends one <see cref="CnaSpriteDrawCommand"/> to
    /// <see cref="_commandBuffer"/>; no native call happens here at all anymore, see this type's
    /// own doc comment. Takes <paramref name="caller"/> rather than hardcoding a name in
    /// <see cref="EnsureHasBegun"/>'s call, since both <c>Draw</c> and <c>DrawString</c> funnel
    /// through this same private method -- hardcoding one name here would misattribute a
    /// no-<c>Begin</c> failure from the other caller.</summary>
    private void DrawEx(
        Texture texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth,
        string caller)
    {
        ArgumentNullException.ThrowIfNull(texture);
        EnsureHasBegun(caller);

        // Only a null source rectangle needs the texture's extents, and then only from the cache.
        // This used to call cna_texture2d_get_info unconditionally -- once per sprite per frame --
        // and then discard the answer whenever the caller had supplied a source rectangle.
        Rectangle source;
        if (sourceRectangle.HasValue)
        {
            source = sourceRectangle.GetValueOrDefault();
        }
        else
        {
            (int textureWidth, int textureHeight) = texture.CachedDimensions;
            source = new Rectangle(0, 0, textureWidth, textureHeight);
        }

        AddDrawCommand(texture, position, source, color, rotation, origin, scale, effects, layerDepth);
    }

    /// <summary>
    /// Buffers one already-resolved sprite, and keeps its texture reachable until the flush.
    ///
    /// The reachability set stays exact -- every distinct texture drawn this batch is in it -- but
    /// a batch normally draws long runs of one texture, so a run is collapsed by one reference
    /// comparison before the set is consulted at all. <see cref="_referencedTextures"/> hashes on
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(object)"/>, a real
    /// call that was being paid thousands of times a frame to re-add the same object.
    /// </summary>
    private void AddDrawCommand(
        Texture texture,
        Vector2 position,
        Rectangle source,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        if (!ReferenceEquals(texture, _lastReferencedTexture))
        {
            _referencedTextures.Add(texture);
            _lastReferencedTexture = texture;
        }

        _commandBuffer.Add(new CnaSpriteDrawCommand(
            new CnaHandle(texture.NativeHandleValue),
            position.ToNative(),
            source.ToNative(),
            color.ToNative(),
            rotation,
            origin.ToNative(),
            scale.ToNative(),
            (uint)effects,
            layerDepth));
    }

    /// <summary>The destination-rectangle overloads' primitive: XNA specifies these by the
    /// screen-space rectangle the sprite should fill rather than by position+scale, so this
    /// resolves that rectangle (and the source-vs-whole-texture size it is scaled against) down
    /// to the position+scale form <see cref="DrawEx(Texture,Vector2,Rectangle?,Color,float,Vector2,Vector2,SpriteEffects,float,string)"/>
    /// expects, then delegates to it -- the actual native call happens there, not here.</summary>
    private void DrawEx(
        Texture texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth,
        string caller)
    {
        ArgumentNullException.ThrowIfNull(texture);
        EnsureHasBegun(caller);

        // The source rectangle is resolved here and handed down already resolved. Before this the
        // extents were read from native here and then read a second time by the position-based
        // DrawEx below, so every destination-rectangle sprite cost two ABI transitions rather than
        // the none it costs now.
        Rectangle source;
        if (sourceRectangle.HasValue)
        {
            source = sourceRectangle.GetValueOrDefault();
        }
        else
        {
            (int textureWidth, int textureHeight) = texture.CachedDimensions;
            source = new Rectangle(0, 0, textureWidth, textureHeight);
        }

        var scale = new Vector2(
            source.Width == 0 ? 0f : destinationRectangle.Width / (float)source.Width,
            source.Height == 0 ? 0f : destinationRectangle.Height / (float)source.Height);

        AddDrawCommand(
            texture,
            new Vector2(destinationRectangle.X, destinationRectangle.Y),
            source,
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
    /// applied once per glyph. Reuses a single per-<c>SpriteBatch</c> list across calls (cleared,
    /// not reallocated) rather than allocating one per <c>DrawString</c> call, since this runs in
    /// the per-frame render path.</summary>
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
        EnsureHasBegun(nameof(DrawString));

        // The native text route when the font can supply a handle and this renderer has not already
        // refused it. Measured on the authored fixture: identical glyph placement, 0 of 1024 pixels
        // different, and about 40% less time per text-heavy frame -- not because it makes fewer ABI
        // crossings (it makes one per string where the buffer made one per batch) but because it
        // does not compute and buffer a quad per glyph. See plan.md A1c.
        if (!_nativeTextRefused && spriteFont.NativeFontHandleValue is var fontHandle and not 0)
        {
            _referencedFonts.Add(spriteFont);
            _pendingText.Add(new PendingText(
                _commandBuffer.Count, spriteFont, fontHandle, text,
                position, color, rotation, origin, scale, effects, layerDepth));
            return;
        }

        _glyphPlacementBuffer.Clear();
        spriteFont.AppendGlyphPlacements(text, _glyphPlacementBuffer);

        foreach (SpriteFont.GlyphPlacement placement in _glyphPlacementBuffer)
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
                layerDepth,
                nameof(DrawString));
        }
    }

    /// <summary>XNA leaves the batch begun when setup or flushing throws; only a successful End
    /// closes the pair. This matters for the observable state after an invalid sort mode or a
    /// rendering failure.</summary>
    public void End()
    {
        EnsureHasBegun(nameof(End));

        FlushCommandBuffer();

        CnaResult result = Native.cna_sprite_batch_end(new CnaHandle(NativeHandleValue));

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(End));
        _hasBegun = false;
    }

    /// <summary>
    /// Replays the batch in issue order -- a no-op when nothing was drawn, matching real XNA's "an
    /// empty batch does nothing" rather than issuing a zero-command native call.
    ///
    /// Sprites leave through one <c>cna_sprite_batch_submit_scaled_many</c> per contiguous run and
    /// strings through <c>cna_sprite_batch_draw_string</c> between them, because the two routes are
    /// separate and the renderer must still see them in the order the game issued them. With no
    /// strings this is exactly the single call it always was.
    /// </summary>
    private void FlushCommandBuffer()
    {
        if (_commandBuffer.Count == 0 && _pendingText.Count == 0)
        {
            return;
        }

        int submitted = 0;
        foreach (PendingText text in _pendingText)
        {
            SubmitSprites(submitted, text.SpriteCountBefore - submitted);
            submitted = text.SpriteCountBefore;
            SubmitText(text);
        }

        SubmitSprites(submitted, _commandBuffer.Count - submitted);

        _commandBuffer.Clear();
        _pendingText.Clear();
        _referencedTextures.Clear();
        _lastReferencedTexture = null;
        _referencedFonts.Clear();
    }

    private unsafe void SubmitSprites(int start, int count)
    {
        if (count <= 0)
        {
            return;
        }

        fixed (CnaSpriteDrawCommand* commands = CollectionsMarshal.AsSpan(_commandBuffer).Slice(start, count))
        {
            CnaResult result = Native.cna_sprite_batch_submit_scaled_many(
                new CnaHandle(NativeHandleValue), commands, (ulong)count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(End));
        }
    }

    /// <summary>
    /// One queued string through the native text route, falling back to glyph quads if the renderer
    /// refuses it.
    ///
    /// The fallback is what makes adopting this safe. Not every renderer has to implement the text
    /// route, and before this change every renderer could draw text, so a refusal must cost speed
    /// rather than the string. The glyphs are expanded and submitted immediately, in this string's
    /// place, so the order the game issued is still the order the renderer sees.
    /// </summary>
    private void SubmitText(PendingText text)
    {
        if (!_nativeTextRefused)
        {
            CnaResult result = CnaStringMarshal.WithStringView(text.Text, view =>
            {
                CnaSpriteTextCommand command = CnaSpriteTextCommand.Versioned();
                command.SpriteFont = new CnaHandle(text.FontHandleValue);
                command.Text = view;
                command.Position = text.Position.ToNative();
                command.Color = text.Color.ToNative();
                command.Rotation = text.Rotation;
                command.Origin = text.Origin.ToNative();
                command.Scale = text.Scale.ToNative();
                command.Effects = (uint)text.Effects;
                command.LayerDepth = text.LayerDepth;

                return Native.cna_sprite_batch_draw_string(new CnaHandle(NativeHandleValue), in command);
            });

            GC.KeepAlive(this);

            if (result == CnaResult.Success)
            {
                return;
            }

            if (result != CnaResult.NotSupported)
            {
                CnaException.ThrowIfFailed(result, nameof(DrawString));
            }

            _nativeTextRefused = true;
        }

        SubmitTextAsGlyphQuads(text);
    }

    private unsafe void SubmitTextAsGlyphQuads(PendingText text)
    {
        var glyphs = new List<SpriteFont.GlyphPlacement>();
        text.Font.AppendGlyphPlacements(text.Text, glyphs);
        if (glyphs.Count == 0)
        {
            return;
        }

        var commands = new CnaSpriteDrawCommand[glyphs.Count];
        for (int i = 0; i < glyphs.Count; i++)
        {
            commands[i] = new CnaSpriteDrawCommand(
                new CnaHandle(text.Font.Texture.NativeHandleValue),
                text.Position.ToNative(),
                glyphs[i].SourceRectangle.ToNative(),
                text.Color.ToNative(),
                text.Rotation,
                (text.Origin - glyphs[i].Anchor).ToNative(),
                text.Scale.ToNative(),
                (uint)text.Effects,
                text.LayerDepth);
        }

        fixed (CnaSpriteDrawCommand* first = commands)
        {
            CnaResult result = Native.cna_sprite_batch_submit_scaled_many(
                new CnaHandle(NativeHandleValue), first, (ulong)commands.Length);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(DrawString));
        }
    }

    /// <summary>A string waiting for <see cref="FlushCommandBuffer"/>, and where in the sprite
    /// stream it belongs. The text is held as a managed string rather than a marshalled view
    /// because the native view is only valid for the duration of one call.</summary>
    private readonly record struct PendingText(
        int SpriteCountBefore,
        SpriteFont Font,
        nint FontHandleValue,
        string Text,
        Vector2 Position,
        Color Color,
        float Rotation,
        Vector2 Origin,
        Vector2 Scale,
        SpriteEffects Effects,
        float LayerDepth);

    private void EnsureHasBegun(string caller)
    {
        if (!_hasBegun)
        {
            throw new InvalidOperationException($"Begin must be called before {caller}.");
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
