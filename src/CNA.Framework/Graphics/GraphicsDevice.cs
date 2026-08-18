using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Every method resolves a fresh native device handle via <see cref="ResolveNativeDeviceHandle"/>
/// before using it, rather than caching one -- the real, shipped openeggbert/cna C API documents
/// the device handle <c>cna_game_get_graphics_device</c> returns as valid only until the lifecycle
/// callback that fetched it returns (confirmed directly against the real test suite, not just the
/// header doc comment: calling it outside a callback fails, and a handle captured during one
/// callback goes stale once a later, separate callback begins -- see
/// <c>Game.GetNativeGraphicsDeviceHandle</c>'s own doc comment for the concrete evidence). Every
/// <see cref="GraphicsDevice"/> method is only ever called from within a game's own
/// <c>Update</c>/<c>Draw</c>/<c>LoadContent</c>/<c>Initialize</c> override -- themselves real
/// lifecycle callbacks -- so resolving fresh, synchronously, at the top of each method stays inside
/// the same callback invocation the whole time, which is exactly what the real ABI requires.
/// </summary>
public class GraphicsDevice
{
    /// <summary>
    /// The raw native *game* handle value -- not the device handle; see this class's own doc
    /// comment for why holding a cached device handle across calls is unsafe under the real ABI.
    /// <c>protected internal</c> (not <c>internal</c>) so CNA.XnaCompat's <c>GraphicsDevice</c>
    /// subclass constructor can forward it to <c>base()</c> without CNA.XnaCompat ever naming
    /// <see cref="CnaHandle"/> -- see docs/architecture.md. Also read directly by every other
    /// native-backed <c>CNA.Framework</c> resource type (<c>VertexBuffer</c>, <c>IndexBuffer</c>,
    /// <c>Texture2D</c>, <c>RenderTarget2D</c>, <c>SpriteBatch</c>, <c>BasicEffect</c>) through
    /// <see cref="ResolveNativeDeviceHandle"/> rather than a cached field of their own, for the same
    /// reason.
    /// </summary>
    protected internal nint NativeGameHandleValue { get; }

    protected internal GraphicsDevice(nint nativeGameHandleValue)
    {
        NativeGameHandleValue = nativeGameHandleValue;
    }

    /// <summary>Resolves a fresh, currently-valid native device handle for this call only -- see
    /// this class's own doc comment. Every other native-backed resource type in
    /// <c>CNA.Framework</c> that needs a device handle to create itself calls this too (through the
    /// owning <see cref="GraphicsDevice"/> instance), rather than reading a cached handle of its
    /// own.</summary>
    internal CnaHandle ResolveNativeDeviceHandle()
    {
        CnaResult result = Native.cna_game_get_graphics_device(new CnaHandle(NativeGameHandleValue), out CnaHandle device);
        CnaException.ThrowIfFailed(result, "cna_game_get_graphics_device");
        return device;
    }

    /// <summary>Matches real XNA's own simple <c>Clear(Color)</c> overload, which clears only the
    /// color (target) buffer -- <c>cna_graphics_device_clear_options</c> is the real ABI's general
    /// clear route (a third, narrower <c>cna_graphics_device_clear_rgba</c> also exists, taking
    /// four separate 0..1 float channels instead of a <see cref="CnaColor"/>, and a
    /// <c>cna_graphics_device_clear_color_depth</c> also exists for clearing color+depth together --
    /// neither is what this overload needs). <paramref name="color"/>'s depth/stencil arguments are
    /// required by the native call even though the depth/stencil bits are not selected -- passed as
    /// the documented default values (full depth, zero stencil) real XNA's own simple overload uses
    /// internally too.</summary>
    public void Clear(Color color)
    {
        CnaResult result = Native.cna_graphics_device_clear_options(
            ResolveNativeDeviceHandle(), CnaClearOptions.Target, color.ToNative(), 1.0f, 0);
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    /// <summary>
    /// Sets the active render target, or restores the back buffer when <paramref name="renderTarget"/>
    /// is <c>null</c>. Takes <see cref="Texture2D"/> rather than the stricter
    /// <see cref="RenderTarget2D"/> -- a deliberate, documented looseness (real XNA's signature
    /// is <c>SetRenderTarget(RenderTarget2D)</c>) that lets CNA.XnaCompat's <c>RenderTarget2D</c>
    /// (which inherits from CNA.XnaCompat's own <c>Texture2D</c>, not this project's
    /// <see cref="RenderTarget2D"/> -- see that type's doc comment) upcast straight into this
    /// parameter with no override needed, matching every other XnaCompat <c>Draw</c>/<c>Clear</c>
    /// overload's "inherited unchanged, converts through implicit operators" pattern. Whoever calls
    /// this is expected to pass an actual <see cref="RenderTarget2D"/> (or a compat subclass of
    /// one) -- passing a plain <see cref="Texture2D"/> is a caller error the real native call now
    /// catches on its own (it validates the handle really is a render target), unlike before this
    /// migration when nothing could.
    /// </summary>
    public void SetRenderTarget(Texture2D? renderTarget)
    {
        CnaHandle handle = renderTarget is null ? CnaHandle.Zero : new CnaHandle(renderTarget.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_render_target2d(ResolveNativeDeviceHandle(), handle);
        CnaException.ThrowIfFailed(result, nameof(SetRenderTarget));
    }

    public void SetVertexBuffer(VertexBuffer? vertexBuffer)
    {
        CnaHandle handle = vertexBuffer is null ? CnaHandle.Zero : new CnaHandle(vertexBuffer.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_vertex_buffer(ResolveNativeDeviceHandle(), handle);
        CnaException.ThrowIfFailed(result, nameof(SetVertexBuffer));
    }

    private BlendState? _blendState;

    /// <summary>Lazily queries the device's current state on first read (via
    /// <c>cna_graphics_device_get_blend_state</c>) rather than defaulting to
    /// <see cref="Graphics.BlendState.Opaque"/> locally -- the real ABI is the source of truth for
    /// what a freshly created device actually starts with, not an assumption made here.</summary>
    public BlendState BlendState
    {
        get
        {
            if (_blendState is null)
            {
                CnaResult queryResult = Native.cna_graphics_device_get_blend_state(ResolveNativeDeviceHandle(), out CnaBlendState native);
                CnaException.ThrowIfFailed(queryResult, nameof(BlendState));
                _blendState = BlendState.FromNative(native);
            }

            return _blendState;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_blend_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(BlendState));
            _blendState = value;
        }
    }

    private DepthStencilState? _depthStencilState;

    public DepthStencilState DepthStencilState
    {
        get
        {
            if (_depthStencilState is null)
            {
                CnaResult queryResult = Native.cna_graphics_device_get_depth_stencil_state(ResolveNativeDeviceHandle(), out CnaDepthStencilState native);
                CnaException.ThrowIfFailed(queryResult, nameof(DepthStencilState));
                _depthStencilState = DepthStencilState.FromNative(native);
            }

            return _depthStencilState;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_depth_stencil_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(DepthStencilState));
            _depthStencilState = value;
        }
    }

    private RasterizerState? _rasterizerState;

    public RasterizerState RasterizerState
    {
        get
        {
            if (_rasterizerState is null)
            {
                CnaResult queryResult = Native.cna_graphics_device_get_rasterizer_state(ResolveNativeDeviceHandle(), out CnaRasterizerState native);
                CnaException.ThrowIfFailed(queryResult, nameof(RasterizerState));
                _rasterizerState = RasterizerState.FromNative(native);
            }

            return _rasterizerState;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_rasterizer_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(RasterizerState));
            _rasterizerState = value;
        }
    }

    private IndexBuffer? _indices;

    public IndexBuffer? Indices
    {
        get => _indices;
        set
        {
            CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
            CnaResult result = Native.cna_graphics_device_set_index_buffer(ResolveNativeDeviceHandle(), handle);
            CnaException.ThrowIfFailed(result, nameof(Indices));
            _indices = value;
        }
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startVertex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(primitiveCount);

        CnaResult result = Native.cna_graphics_device_draw_primitives(
            ResolveNativeDeviceHandle(), (int)primitiveType, startVertex, primitiveCount);
        CnaException.ThrowIfFailed(result, nameof(DrawPrimitives));
    }

    /// <summary><paramref name="minVertexIndex"/>/<paramref name="numVertices"/> now forward to
    /// native code -- the real <c>cna_graphics_device_draw_indexed_primitives</c> takes exactly
    /// real XNA's own full 7-argument signature (confirmed by reading <c>graphics_device.h</c>
    /// directly), unlike the old guessed 5-argument shape this method used to call, which silently
    /// dropped these two.</summary>
    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseVertex);
        ArgumentOutOfRangeException.ThrowIfNegative(minVertexIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(numVertices);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(primitiveCount);

        CnaResult result = Native.cna_graphics_device_draw_indexed_primitives(
            ResolveNativeDeviceHandle(), (int)primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);
        CnaException.ThrowIfFailed(result, nameof(DrawIndexedPrimitives));
    }
}
