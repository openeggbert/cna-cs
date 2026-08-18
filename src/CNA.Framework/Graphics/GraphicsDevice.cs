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

    /// <summary>Matches real XNA's full <c>Clear(ClearOptions, Color, float, int)</c> overload --
    /// unlike the simple <see cref="Clear(Color)"/> overload above, <paramref name="options"/> is
    /// the caller's own bitmask (cast straight across, since <see cref="ClearOptions"/>'s values
    /// match <see cref="CnaClearOptions"/> exactly) instead of a hardcoded
    /// <see cref="CnaClearOptions.Target"/>-only value.</summary>
    public void Clear(ClearOptions options, Color color, float depth, int stencil)
    {
        CnaResult result = Native.cna_graphics_device_clear_options(
            ResolveNativeDeviceHandle(), (CnaClearOptions)options, color.ToNative(), depth, stencil);
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    public Viewport Viewport
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_get_viewport(ResolveNativeDeviceHandle(), out CnaViewport native);
            CnaException.ThrowIfFailed(result, nameof(Viewport));
            return Viewport.FromNative(native);
        }
        set
        {
            CnaResult result = Native.cna_graphics_device_set_viewport(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(Viewport));
        }
    }

    /// <summary>The adapter this device renders with. Real XNA also has a static
    /// <c>GraphicsAdapter.Adapters</c>; see <see cref="Graphics.GraphicsAdapter"/>'s own doc
    /// comment for why the enumeration entry points here take a device instead.</summary>
    public GraphicsAdapter Adapter => CreateAdapter(GetAdapterIndex());

    /// <summary>The index of the adapter *this device* renders with.
    /// <c>cna_graphics_device_get_adapter_index</c> is the authoritative answer -- deliberately
    /// not a scan for the <c>is_default_adapter</c> flag, which answers "the machine's default
    /// adapter", a different question this device need not agree with.
    ///
    /// Separate from <see cref="CreateAdapter"/> so the override below receives the index as a
    /// parameter rather than having to look it up: an override written as
    /// <c>new GraphicsAdapter(this, base.Adapter.AdapterIndex)</c> would recurse forever, since
    /// <see cref="Adapter"/> dispatches straight back into the override.</summary>
    protected uint GetAdapterIndex()
    {
        CnaResult result = Native.cna_graphics_device_get_adapter_index(ResolveNativeDeviceHandle(), out uint adapterIndex);
        CnaException.ThrowIfFailed(result, nameof(Adapter));
        return adapterIndex;
    }

    /// <summary>Covariant-return factory hook, same pattern as <see cref="QueryBlendState"/>.</summary>
    protected virtual GraphicsAdapter CreateAdapter(uint adapterIndex) => new(this, adapterIndex);

    /// <summary>The device's current presentation configuration. The setter applies it through
    /// <c>cna_graphics_device_set_presentation_parameters</c>; mutating the object a getter
    /// returned does nothing until it is assigned back, matching real XNA.</summary>
    public PresentationParameters PresentationParameters
    {
        get
        {
            var native = new CnaPresentationParameters();
            CnaResult result = Native.cna_graphics_device_get_presentation_parameters(ResolveNativeDeviceHandle(), ref native);
            CnaException.ThrowIfFailed(result, nameof(PresentationParameters));
            return WrapPresentationParameters(PresentationParameters.FromNative(native));
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_presentation_parameters(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(PresentationParameters));
        }
    }

    /// <summary>Re-typing hook for CNA.XnaCompat -- takes and returns the public type rather than
    /// the interop struct, for the same CS0050 reason
    /// <see cref="SamplerStateCollection.Wrap"/> documents.</summary>
    protected virtual PresentationParameters WrapPresentationParameters(PresentationParameters parameters) => parameters;

    /// <summary>The display mode this device is currently presenting in.</summary>
    public DisplayMode DisplayMode
    {
        get
        {
            var native = new CnaDisplayMode();
            CnaResult result = Native.cna_graphics_device_get_display_mode(ResolveNativeDeviceHandle(), ref native);
            CnaException.ThrowIfFailed(result, nameof(DisplayMode));
            return Graphics.DisplayMode.FromNative(in native);
        }
    }

    public GraphicsProfile GraphicsProfile
    {
        get
        {
            CnaResult result = Native.cna_graphics_device_get_graphics_profile(ResolveNativeDeviceHandle(), out CnaGraphicsProfile native);
            CnaException.ThrowIfFailed(result, nameof(GraphicsProfile));
            return (GraphicsProfile)native;
        }
    }

    /// <summary>Matches real XNA's <c>DrawUserPrimitives&lt;T&gt;</c>. Only the four vertex types
    /// the real ABI's own <c>CNA_UserVertexSource</c> names -- <see cref="VertexPositionColor"/>,
    /// <see cref="VertexPositionColorTexture"/>, <see cref="VertexPositionTexture"/>,
    /// <see cref="VertexPositionNormalTexture"/> -- are supported; any other <typeparamref name="T"/>
    /// would need the raw-stream route with a native vertex-declaration resource this project
    /// doesn't have (see <see cref="UserVertexSource"/>'s own doc comment).</summary>
    public unsafe void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(vertexData);

        UserVertexSource vertexSource = VertexSourceFor<T>();

        fixed (T* vertexDataPtr = vertexData)
        {
            DrawUserPrimitivesRaw(primitiveType, vertexDataPtr, vertexSource, vertexOffset, primitiveCount);
        }
    }

    private static UserVertexSource VertexSourceFor<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(VertexPositionColor))
        {
            return UserVertexSource.PositionColor;
        }

        if (typeof(T) == typeof(VertexPositionColorTexture))
        {
            return UserVertexSource.PositionColorTexture;
        }

        if (typeof(T) == typeof(VertexPositionTexture))
        {
            return UserVertexSource.PositionTexture;
        }

        if (typeof(T) == typeof(VertexPositionNormalTexture))
        {
            return UserVertexSource.PositionNormalTexture;
        }

        throw new NotSupportedException(
            $"DrawUserPrimitives<{typeof(T).Name}> is not supported -- only VertexPositionColor, " +
            "VertexPositionColorTexture, VertexPositionTexture, and VertexPositionNormalTexture match a real " +
            "CNA_USER_VERTEX_SOURCE_* identity.");
    }

    /// <summary>The pointer-and-identity level <see cref="DrawUserPrimitives{T}"/> builds on --
    /// <c>protected</c>, not <c>private</c>, so CNA.XnaCompat's <c>GraphicsDevice</c> override can
    /// reach it for its own, separately-typed compat vertex structs (structs can't share a type
    /// across the CNA/XnaCompat boundary the way <see cref="RenderTarget2D"/>-style reference types
    /// do -- see <see cref="Graphics.UserVertexSource"/>'s own doc comment) without ever naming a
    /// <c>CNA.Interop</c> type.</summary>
    protected unsafe void DrawUserPrimitivesRaw(
        PrimitiveType primitiveType, void* vertexData, UserVertexSource vertexSource, int vertexOffset, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexOffset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(primitiveCount);

        var primitives = new CnaUserPrimitives
        {
            PrimitiveType = (int)primitiveType,
            VertexSource = (CnaUserVertexSource)vertexSource,
            VertexData = vertexData,
            VertexDeclaration = CnaHandle.Zero,
            VertexOffset = vertexOffset,
            PrimitiveCount = primitiveCount,
        };

        CnaResult result = Native.cna_graphics_device_draw_user_primitives(ResolveNativeDeviceHandle(), in primitives);
        CnaException.ThrowIfFailed(result, nameof(DrawUserPrimitives));
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
        GC.KeepAlive(renderTarget);
        CnaException.ThrowIfFailed(result, nameof(SetRenderTarget));
    }

    /// <summary>Matches real XNA's <c>SetRenderTarget(RenderTargetCube, CubeMapFace)</c>. A
    /// separate method from <see cref="SetRenderTarget(Texture2D?)"/> because the real ABI's
    /// render-target binding is shape-specific: <c>cna_graphics_device_set_render_target_cube</c>
    /// takes the face to render into, which has no 2D equivalent. Passing <see langword="null"/>
    /// restores the back buffer, same sentinel as the 2D overload.</summary>
    public void SetRenderTarget(RenderTargetCube? renderTarget, CubeMapFace cubeMapFace)
    {
        CnaHandle handle = renderTarget is null ? CnaHandle.Zero : new CnaHandle(renderTarget.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_render_target_cube(
            ResolveNativeDeviceHandle(), handle, (uint)cubeMapFace);
        GC.KeepAlive(renderTarget);
        CnaException.ThrowIfFailed(result, nameof(SetRenderTarget));
    }

    public void SetVertexBuffer(VertexBuffer? vertexBuffer)
    {
        CnaHandle handle = vertexBuffer is null ? CnaHandle.Zero : new CnaHandle(vertexBuffer.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_vertex_buffer(ResolveNativeDeviceHandle(), handle);
        GC.KeepAlive(vertexBuffer);
        CnaException.ThrowIfFailed(result, nameof(SetVertexBuffer));
    }

    /// <summary>Matches real XNA's <c>SetVertexBuffers(params VertexBufferBinding[])</c> -- the
    /// multi-stream form <see cref="SetVertexBuffer"/> is shorthand for. Passing no bindings (or
    /// <see langword="null"/>) unbinds every stream, which is what the native call documents an
    /// empty array as doing.</summary>
    public unsafe void SetVertexBuffers(params VertexBufferBinding[]? vertexBuffers)
    {
        if (vertexBuffers is null || vertexBuffers.Length == 0)
        {
            CnaResult emptyResult = Native.cna_graphics_device_set_vertex_buffers(ResolveNativeDeviceHandle(), null, 0);
            CnaException.ThrowIfFailed(emptyResult, nameof(SetVertexBuffers));
            return;
        }

        var native = new CnaVertexBufferBinding[vertexBuffers.Length];
        for (int i = 0; i < native.Length; i++)
        {
            native[i] = vertexBuffers[i].ToNative();
        }

        fixed (CnaVertexBufferBinding* nativePtr = native)
        {
            CnaResult result = Native.cna_graphics_device_set_vertex_buffers(
                ResolveNativeDeviceHandle(), nativePtr, (ulong)native.Length);

            // The native array holds bare handles; vertexBuffers is what keeps the VertexBuffer
            // objects (and so their SafeHandles) reachable across the call.
            GC.KeepAlive(vertexBuffers);
            CnaException.ThrowIfFailed(result, nameof(SetVertexBuffers));
        }
    }

    private BlendState? _blendState;

    /// <summary>Lazily queries the device's current state on first read (via
    /// <c>cna_graphics_device_get_blend_state</c>) rather than defaulting to
    /// <see cref="Graphics.BlendState.Opaque"/> locally -- the real ABI is the source of truth for
    /// what a freshly created device actually starts with, not an assumption made here. The query
    /// itself is split into <see cref="QueryBlendState"/>, a <see langword="protected virtual"/>
    /// hook -- CNA.XnaCompat's <c>GraphicsDevice</c> overrides it to return a compat-typed
    /// <c>BlendState</c> instead, so <c>this.GraphicsDevice.BlendState</c> never throws
    /// <see cref="InvalidCastException"/> on a first read that happens before any explicit
    /// <see langword="set"/> -- the same class of bug <see cref="Indices"/>'s own doc comment
    /// describes, but for a property whose base default is a real, non-null constructed value
    /// rather than <see langword="null"/>.</summary>
    public BlendState BlendState
    {
        get => _blendState ??= QueryBlendState();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_blend_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(BlendState));
            _blendState = value;
        }
    }

    protected virtual BlendState QueryBlendState()
    {
        CnaResult queryResult = Native.cna_graphics_device_get_blend_state(ResolveNativeDeviceHandle(), out CnaBlendState native);
        CnaException.ThrowIfFailed(queryResult, nameof(BlendState));
        return BlendState.FromNative(native);
    }

    private DepthStencilState? _depthStencilState;

    /// <summary>See <see cref="BlendState"/>'s own doc comment for why the default-construction
    /// path is a separate, overridable <see cref="QueryDepthStencilState"/> hook.</summary>
    public DepthStencilState DepthStencilState
    {
        get => _depthStencilState ??= QueryDepthStencilState();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_depth_stencil_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(DepthStencilState));
            _depthStencilState = value;
        }
    }

    protected virtual DepthStencilState QueryDepthStencilState()
    {
        CnaResult queryResult = Native.cna_graphics_device_get_depth_stencil_state(ResolveNativeDeviceHandle(), out CnaDepthStencilState native);
        CnaException.ThrowIfFailed(queryResult, nameof(DepthStencilState));
        return DepthStencilState.FromNative(native);
    }

    private RasterizerState? _rasterizerState;

    /// <summary>See <see cref="BlendState"/>'s own doc comment for why the default-construction
    /// path is a separate, overridable <see cref="QueryRasterizerState"/> hook.</summary>
    public RasterizerState RasterizerState
    {
        get => _rasterizerState ??= QueryRasterizerState();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_graphics_device_set_rasterizer_state(ResolveNativeDeviceHandle(), value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(RasterizerState));
            _rasterizerState = value;
        }
    }

    protected virtual RasterizerState QueryRasterizerState()
    {
        CnaResult queryResult = Native.cna_graphics_device_get_rasterizer_state(ResolveNativeDeviceHandle(), out CnaRasterizerState native);
        CnaException.ThrowIfFailed(queryResult, nameof(RasterizerState));
        return RasterizerState.FromNative(native);
    }

    private SamplerStateCollection? _samplerStates;
    private SamplerStateCollection? _vertexSamplerStates;

    /// <summary>The pixel shader's sampler collection. Unlike
    /// <see cref="BlendState"/>/<see cref="DepthStencilState"/>/<see cref="RasterizerState"/>,
    /// what is cached here is the *collection object*, not any state value -- the collection
    /// itself reads and writes through to native on every access (see its own doc comment).</summary>
    public SamplerStateCollection SamplerStates => _samplerStates ??= CreateSamplerStateCollection(vertexStage: false);

    public SamplerStateCollection VertexSamplerStates => _vertexSamplerStates ??= CreateSamplerStateCollection(vertexStage: true);

    /// <summary>Covariant-return factory hook, same pattern as <see cref="QueryBlendState"/> --
    /// CNA.XnaCompat overrides it to build its own compat-typed collection.</summary>
    protected virtual SamplerStateCollection CreateSamplerStateCollection(bool vertexStage) => new(this, vertexStage);

    private TextureCollection? _textures;
    private TextureCollection? _vertexTextures;

    /// <summary>The pixel shader's texture bindings. Unlike <see cref="SamplerStates"/>, the
    /// collection object here is genuinely stateful -- see <see cref="TextureCollection"/>'s own
    /// doc comment for why native cannot answer its getter.</summary>
    public TextureCollection Textures => _textures ??= CreateTextureCollection(vertexStage: false);

    public TextureCollection VertexTextures => _vertexTextures ??= CreateTextureCollection(vertexStage: true);

    protected virtual TextureCollection CreateTextureCollection(bool vertexStage) => new(this, vertexStage);

    private IndexBuffer? _indices;

    public IndexBuffer? Indices
    {
        get => _indices;
        set
        {
            CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
            CnaResult result = Native.cna_graphics_device_set_index_buffer(ResolveNativeDeviceHandle(), handle);
        GC.KeepAlive(value);
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
