namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>GraphicsDevice</c>. A pure subclass -- <c>Clear(Color)</c> is inherited
/// unchanged from <see cref="CNA.Graphics.GraphicsDevice"/> and resolves correctly
/// against this namespace's <see cref="Color"/> argument through that struct's implicit
/// conversion operator, so no override is needed here. See docs/architecture.md.
/// </summary>
public class GraphicsDevice : CNA.Graphics.GraphicsDevice
{
    protected internal GraphicsDevice(nint nativeGameHandleValue)
        : base(nativeGameHandleValue)
    {
    }

    /// <summary>
    /// <c>SetVertexBuffer</c> is inherited unchanged (its <c>VertexBuffer</c> argument upcasts,
    /// same as every other compat method taking a native-backed resource type). <c>Indices</c>
    /// needs a `new` override since its declared type differs from the base property's -- but
    /// deliberately holds *no field of its own*: an earlier draft gave this property its own
    /// private backing field (mirroring <c>SetData</c>-style patterns elsewhere), which desyncs
    /// from the base class's own field whenever the object is accessed through a base-typed
    /// reference (e.g. <c>GraphicsDeviceManager.Game</c> is declared with the base <c>CNA.Game</c>
    /// type, so <c>manager.Game.GraphicsDevice.Indices</c> would silently read/write a different
    /// field than <c>this.GraphicsDevice.Indices</c> inside a <c>Game</c> subclass) -- caught by
    /// a code-review pass, not by testing (the desync needs two different static-type access
    /// paths to the same object to manifest, which no test here exercises). Fixed by making this
    /// a pure downcast pass-through to the base property's own single field instead, the same
    /// "no independent state, just a typed read/write-through" pattern
    /// <see cref="Microsoft.Xna.Framework.Audio.SoundEffectInstance.State"/> already uses.
    /// </summary>
    /// <summary>Re-typed: <see cref="GraphicsCapability"/> is a separate enum per namespace, like
    /// every other CNA enum the compat layer mirrors. Without this a compat game calling
    /// <c>SupportsCapability</c> would have to name a <c>CNA.Graphics</c> type.</summary>
    public bool SupportsCapability(GraphicsCapability capability) =>
        base.SupportsCapability((CNA.Graphics.GraphicsCapability)(uint)capability);

    public new IndexBuffer? Indices
    {
        get => (IndexBuffer?)base.Indices;
        set => base.Indices = value;
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount) =>
        base.DrawPrimitives((CNA.Graphics.PrimitiveType)(int)primitiveType, startVertex, primitiveCount);

    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount) =>
        base.DrawIndexedPrimitives(
            (CNA.Graphics.PrimitiveType)(int)primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);

    public void Clear(ClearOptions options, Color color, float depth, int stencil) =>
        base.Clear((CNA.Graphics.ClearOptions)(int)options, color, depth, stencil);

    public new Viewport Viewport
    {
        get => Viewport.FromNative(base.Viewport);
        set => base.Viewport = value.ToNative();
    }

    public new GraphicsProfile GraphicsProfile => (GraphicsProfile)base.GraphicsProfile;

    public void SetVertexBuffers(params VertexBufferBinding[]? vertexBuffers)
    {
        if (vertexBuffers is null)
        {
            base.SetVertexBuffers(null);
            return;
        }

        var converted = new CNA.Graphics.VertexBufferBinding[vertexBuffers.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = vertexBuffers[i].ToFramework();
        }

        base.SetVertexBuffers(converted);
    }

    public new GraphicsAdapter Adapter => (GraphicsAdapter)base.Adapter;

    protected override CNA.Graphics.GraphicsAdapter CreateAdapter(uint adapterIndex) => new GraphicsAdapter(this, adapterIndex);

    public new PresentationParameters PresentationParameters
    {
        get => (PresentationParameters)base.PresentationParameters;
        set => base.PresentationParameters = value;
    }

    protected override CNA.Graphics.PresentationParameters WrapPresentationParameters(CNA.Graphics.PresentationParameters parameters) =>
        new PresentationParameters(parameters);

    public new DisplayMode DisplayMode => DisplayMode.FromFramework(base.DisplayMode);

    /// <summary>Same downcast pass-through pattern as <see cref="Indices"/>, but for a property
    /// whose base default is a real, non-null value -- see the base <c>BlendState</c> property's
    /// own doc comment for why <see cref="QueryBlendState"/> needs its own override here rather
    /// than relying on the downcast alone.</summary>
    public new BlendState BlendState
    {
        get => (BlendState)base.BlendState;
        set => base.BlendState = value;
    }

    protected override CNA.Graphics.BlendState QueryBlendState() => new BlendState(base.QueryBlendState());

    public new DepthStencilState DepthStencilState
    {
        get => (DepthStencilState)base.DepthStencilState;
        set => base.DepthStencilState = value;
    }

    protected override CNA.Graphics.DepthStencilState QueryDepthStencilState() => new DepthStencilState(base.QueryDepthStencilState());

    public new RasterizerState RasterizerState
    {
        get => (RasterizerState)base.RasterizerState;
        set => base.RasterizerState = value;
    }

    protected override CNA.Graphics.RasterizerState QueryRasterizerState() => new RasterizerState(base.QueryRasterizerState());

    public new SamplerStateCollection SamplerStates => (SamplerStateCollection)base.SamplerStates;

    public new SamplerStateCollection VertexSamplerStates => (SamplerStateCollection)base.VertexSamplerStates;

    protected override CNA.Graphics.SamplerStateCollection CreateSamplerStateCollection(bool vertexStage) =>
        new SamplerStateCollection(this, vertexStage);

    public new TextureCollection Textures => (TextureCollection)base.Textures;

    public new TextureCollection VertexTextures => (TextureCollection)base.VertexTextures;

    protected override CNA.Graphics.TextureCollection CreateTextureCollection(bool vertexStage) =>
        new TextureCollection(this, vertexStage);

    /// <summary>Re-typed: <c>Rectangle</c> is a separate struct per namespace. Everything else the
    /// device gained -- <c>Present</c>, <c>Reset</c>, <c>MultiSampleMask</c>,
    /// <c>ReferenceStencil</c>, <c>IsDisposed</c>, <c>DrawInstancedPrimitives</c>, the four
    /// events -- is inherited unchanged, since none of those types diverge.</summary>
    public new Rectangle ScissorRectangle
    {
        get => base.ScissorRectangle;
        set => base.ScissorRectangle = value;
    }

    /// <summary>Re-typed: <c>Color</c> is a separate struct per namespace.</summary>
    public new Color BlendFactor
    {
        get => base.BlendFactor;
        set => base.BlendFactor = value;
    }

    /// <summary>Re-typed: <c>Color</c> and <c>Rectangle</c> are separate structs per
    /// namespace.</summary>
    public void GetBackBufferData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        GetBackBufferData(null, data, 0, data.Length);
    }

    /// <summary>See <see cref="GetBackBufferData(Color[])"/>.</summary>
    public void GetBackBufferData(Rectangle? rect, Color[] data, int startIndex, int elementCount)
    {
        ArgumentNullException.ThrowIfNull(data);

        var converted = new CNA.Color[data.Length];
        base.GetBackBufferData(
            rect is { } r ? (CNA.Rectangle)r : null, converted, startIndex, elementCount);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = converted[i];
        }
    }

    /// <summary>Re-typed: <c>GraphicsDeviceStatus</c> is a separate enum per namespace.</summary>
    public new GraphicsDeviceStatus GraphicsDeviceStatus => (GraphicsDeviceStatus)(int)base.GraphicsDeviceStatus;

    /// <summary>Re-typed: takes this namespace's own <see cref="PresentationParameters"/>.</summary>
    public void Reset(PresentationParameters presentationParameters)
    {
        ArgumentNullException.ThrowIfNull(presentationParameters);
        base.Reset(presentationParameters);
    }

    /// <summary>Re-typed: takes this namespace's own <see cref="PresentationParameters"/> and
    /// <see cref="GraphicsAdapter"/>.</summary>
    public void Reset(PresentationParameters presentationParameters, GraphicsAdapter graphicsAdapter)
    {
        ArgumentNullException.ThrowIfNull(presentationParameters);
        ArgumentNullException.ThrowIfNull(graphicsAdapter);
        base.Reset(presentationParameters, graphicsAdapter);
    }

    /// <summary>Re-typed: <c>PrimitiveType</c> is a separate enum per namespace.</summary>
    public void DrawInstancedPrimitives(
        PrimitiveType primitiveType,
        int baseVertex,
        int minVertexIndex,
        int numVertices,
        int startIndex,
        int primitiveCount,
        int instanceCount) =>
        base.DrawInstancedPrimitives(
            (CNA.Graphics.PrimitiveType)(int)primitiveType, baseVertex, minVertexIndex,
            numVertices, startIndex, primitiveCount, instanceCount);

    /// <summary>Matches real XNA's <c>SetRenderTargets</c>. Re-typed rather than inherited: the
    /// base takes <c>CNA.Graphics.RenderTargetBinding</c>, and this namespace's own is a separate
    /// struct (structs cannot be subclassed), so the array is converted element-wise -- the same
    /// thing this layer does for every array of a type with a conversion operator.</summary>
    public void SetRenderTargets(params RenderTargetBinding[]? renderTargets)
    {
        if (renderTargets is null || renderTargets.Length == 0)
        {
            base.SetRenderTargets();
            return;
        }

        var converted = new CNA.Graphics.RenderTargetBinding[renderTargets.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = renderTargets[i];
        }

        base.SetRenderTargets(converted);
    }

    /// <summary>Matches real XNA's <c>GetRenderTargets</c>. Only the count is meaningful across
    /// this boundary: the base answers from the <c>CNA.Graphics</c> bindings it was handed, and a
    /// compat binding converts one way only, so re-typing them back is not possible. Returns
    /// default-constructed bindings of the right length, which is what a caller checking
    /// <c>Length</c> -- the realistic use -- needs.</summary>
    public new RenderTargetBinding[] GetRenderTargets() => new RenderTargetBinding[base.GetRenderTargets().Length];

    /// <summary>Matches real XNA's <c>DrawUserPrimitives&lt;T&gt;</c>. Reimplements the
    /// type-to-<c>UserVertexSource</c> mapping for this namespace's own vertex structs rather than
    /// forwarding to <see cref="CNA.Graphics.GraphicsDevice.DrawUserPrimitives{T}"/> directly --
    /// compat vertex structs (e.g. <see cref="VertexPositionTexture"/>) are separate types from
    /// their CNA.Graphics counterparts (structs can't be subclassed to share one), so the base
    /// generic method's own <c>typeof(T) ==</c> checks would never match. Goes through
    /// <see cref="CNA.Graphics.GraphicsDevice.DrawUserPrimitivesRaw"/> instead, the shared
    /// pointer-and-identity level both namespaces build on.
    ///
    /// Any <see cref="IVertexType"/> works, not only the four the ABI names -- see
    /// <see cref="CNA.Graphics.UserVertexSource"/> for why the previous restriction was
    /// self-imposed.</summary>
    public unsafe void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(vertexData);

        CNA.Graphics.UserVertexSource? typedSource = TypedVertexSourceFor<T>();

        fixed (T* vertexDataPtr = vertexData)
        {
            DrawUserPrimitivesRaw(
                (CNA.Graphics.PrimitiveType)(int)primitiveType,
                vertexDataPtr,
                typedSource ?? CNA.Graphics.UserVertexSource.RawStream,
                vertexOffset,
                primitiveCount,
                typedSource is null ? DeclarationFor<T>() : null);
        }
    }

    /// <summary>Matches real XNA's <c>DrawUserIndexedPrimitives&lt;T&gt;</c>. Reimplements the
    /// vertex-type mapping for the same reason <see cref="DrawUserPrimitives{T}"/> does -- compat
    /// vertex structs are separate types from their CNA.Graphics counterparts -- and reaches the
    /// shared raw level underneath.</summary>
    public unsafe void DrawUserIndexedPrimitives<TVertex, TIndex>(
        PrimitiveType primitiveType,
        TVertex[] vertexData,
        int vertexOffset,
        int numVertices,
        TIndex[] indexData,
        int indexOffset,
        int primitiveCount)
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        ArgumentNullException.ThrowIfNull(vertexData);
        ArgumentNullException.ThrowIfNull(indexData);

        CNA.Graphics.UserVertexSource? typedSource = TypedVertexSourceFor<TVertex>();
        CNA.Graphics.IndexElementSize indexElementSize = CNA.Graphics.IndexBuffer.SizeForType(typeof(TIndex));

        fixed (TVertex* vertexDataPtr = vertexData)
        fixed (TIndex* indexDataPtr = indexData)
        {
            DrawUserIndexedPrimitivesRaw(
                (CNA.Graphics.PrimitiveType)(int)primitiveType,
                vertexDataPtr,
                typedSource ?? CNA.Graphics.UserVertexSource.RawStream,
                vertexOffset,
                numVertices,
                indexDataPtr,
                indexElementSize,
                indexOffset,
                primitiveCount,
                typedSource is null ? DeclarationFor<TVertex>() : null);
        }
    }

    /// <summary><see langword="null"/> for a vertex type the ABI does not name directly, which is
    /// a fall-through to the raw-stream route rather than a failure -- see
    /// <see cref="CNA.Graphics.UserVertexSource"/> for the header evidence that the raw route was
    /// never actually blocked.</summary>
    private static CNA.Graphics.UserVertexSource? TypedVertexSourceFor<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(VertexPositionColor))
        {
            return CNA.Graphics.UserVertexSource.PositionColor;
        }

        if (typeof(T) == typeof(VertexPositionColorTexture))
        {
            return CNA.Graphics.UserVertexSource.PositionColorTexture;
        }

        if (typeof(T) == typeof(VertexPositionTexture))
        {
            return CNA.Graphics.UserVertexSource.PositionTexture;
        }

        if (typeof(T) == typeof(VertexPositionNormalTexture))
        {
            return CNA.Graphics.UserVertexSource.PositionNormalTexture;
        }

        return null;
    }

    /// <summary>Derives the declaration for a compat vertex type with no
    /// <c>CNA_UserVertexSource</c> identity. Goes through this namespace's own
    /// <see cref="VertexDeclaration"/>, which converts implicitly to the CNA one, so a compat
    /// <see cref="IVertexType"/> is read through the compat interface it actually
    /// implements.</summary>
    private static CNA.Graphics.VertexDeclaration DeclarationFor<T>() where T : unmanaged
    {
        if (Activator.CreateInstance<T>() is not IVertexType instance)
        {
            throw new NotSupportedException(
                $"DrawUserPrimitives<{typeof(T).Name}> needs {typeof(T).Name} to implement IVertexType, so its " +
                "vertex declaration can be derived. Only VertexPositionColor, VertexPositionColorTexture, " +
                "VertexPositionTexture and VertexPositionNormalTexture are drawable without one.");
        }

        return instance.VertexDeclaration.Framework;
    }
}
