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
/// (message text recalled from memory, not independently verified against a live binary or
/// decompiled source -- flagged the same way this session flags other recalled-not-verified
/// details, e.g. the rare <c>Keys</c> ordinals).
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
    private bool _hasBegun;

    public SpriteBatch(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_sprite_batch_create(graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(SpriteBatch));

        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_sprite_batch_destroy(new CnaHandle(h)));
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
    /// LinearClamp, and so on -- which is both what XNA documents and what the ABI's own null
    /// pointer selects, so nothing has to be substituted here.
    /// </summary>
    public unsafe void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState)
    {
        BeginGuard();

        CnaBlendState blend = blendState?.ToNative() ?? default;
        CnaSamplerState sampler = samplerState?.ToNative() ?? default;
        CnaDepthStencilState depthStencil = depthStencilState?.ToNative() ?? default;
        CnaRasterizerState rasterizer = rasterizerState?.ToNative() ?? default;

        CnaResult result = Native.cna_sprite_batch_begin_with_states(
            new CnaHandle(NativeHandleValue),
            (uint)sortMode,
            blendState is null ? null : &blend,
            samplerState is null ? null : &sampler,
            depthStencilState is null ? null : &depthStencil,
            rasterizerState is null ? null : &rasterizer);
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

        CnaBlendState blend = blendState?.ToNative() ?? default;
        CnaSamplerState sampler = samplerState?.ToNative() ?? default;
        CnaDepthStencilState depthStencil = depthStencilState?.ToNative() ?? default;
        CnaRasterizerState rasterizer = rasterizerState?.ToNative() ?? default;
        CnaMatrix transform = transformMatrix?.ToNative() ?? default;

        CnaResult result = Native.cna_sprite_batch_begin_with_effect(
            new CnaHandle(NativeHandleValue),
            (uint)sortMode,
            blendState is null ? null : &blend,
            samplerState is null ? null : &sampler,
            depthStencilState is null ? null : &depthStencil,
            rasterizerState is null ? null : &rasterizer,
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
        _referencedTextures.Clear();
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

        (int textureWidth, int textureHeight) = Texture2D.GetTexture2DDimensions(texture.NativeHandleValue);
        Rectangle source = sourceRectangle ?? new Rectangle(0, 0, textureWidth, textureHeight);

        _referencedTextures.Add(texture);
        _commandBuffer.Add(new CnaSpriteDrawCommand(
            new CnaHandle(texture.NativeHandleValue),
            position.ToNative(),
            source.ToNative(),
            color.ToNative(),
            rotation,
            origin.ToNative(),
            scale.ToNative(),
            (int)effects,
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

        (int textureWidth, int textureHeight) = Texture2D.GetTexture2DDimensions(texture.NativeHandleValue);
        int sourceWidth = sourceRectangle?.Width ?? textureWidth;
        int sourceHeight = sourceRectangle?.Height ?? textureHeight;
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
            layerDepth,
            caller);
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

    /// <summary>
    /// Wraps the flush + native end call in <c>try</c>/<c>finally</c> specifically so a native
    /// failure can't permanently strand this instance: without it, a thrown <see cref="CnaException"/>
    /// would leave <see cref="_hasBegun"/> stuck <c>true</c> forever, since nothing else ever
    /// resets it and there is no public API to do so directly -- every future <see cref="Begin()"/>
    /// call would then throw "cannot be called again until End has been successfully called"
    /// with no way to recover short of disposing this instance and constructing a new one. Resetting
    /// unconditionally lets a caller retry <see cref="Begin()"/> after a failure instead.
    /// </summary>
    public void End()
    {
        EnsureHasBegun(nameof(End));

        try
        {
            FlushCommandBuffer();

            CnaResult result = Native.cna_sprite_batch_end(new CnaHandle(NativeHandleValue));

            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(End));
        }
        finally
        {
            _hasBegun = false;
        }
    }

    /// <summary>The one native call the whole buffered batch flushes through -- a no-op if
    /// nothing was drawn this <c>Begin</c>/<c>End</c> pair, matching real XNA's own "an empty
    /// batch does nothing" behavior rather than issuing a zero-command native call for no
    /// reason.</summary>
    private unsafe void FlushCommandBuffer()
    {
        if (_commandBuffer.Count == 0)
        {
            return;
        }

        fixed (CnaSpriteDrawCommand* commands = CollectionsMarshal.AsSpan(_commandBuffer))
        {
            CnaResult result = Native.cna_sprite_batch_submit_scaled_many(
                new CnaHandle(NativeHandleValue), commands, (ulong)_commandBuffer.Count);
            CnaException.ThrowIfFailed(result, nameof(End));
        }

        _commandBuffer.Clear();
        _referencedTextures.Clear();
    }

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
