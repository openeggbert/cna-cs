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

    /// <summary>Matches real XNA's <c>DrawUserPrimitives&lt;T&gt;</c>. Reimplements the
    /// type-to-<c>UserVertexSource</c> mapping for this namespace's own vertex structs rather than
    /// forwarding to <see cref="CNA.Graphics.GraphicsDevice.DrawUserPrimitives{T}"/> directly --
    /// compat vertex structs (e.g. <see cref="VertexPositionTexture"/>) are separate types from
    /// their CNA.Graphics counterparts (structs can't be subclassed to share one), so the base
    /// generic method's own <c>typeof(T) ==</c> checks would never match. Goes through
    /// <see cref="CNA.Graphics.GraphicsDevice.DrawUserPrimitivesRaw"/> instead, the shared
    /// pointer-and-identity level both namespaces build on.</summary>
    public unsafe void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(vertexData);

        CNA.Graphics.UserVertexSource vertexSource = VertexSourceFor<T>();

        fixed (T* vertexDataPtr = vertexData)
        {
            DrawUserPrimitivesRaw(
                (CNA.Graphics.PrimitiveType)(int)primitiveType, vertexDataPtr, vertexSource, vertexOffset, primitiveCount);
        }
    }

    private static CNA.Graphics.UserVertexSource VertexSourceFor<T>() where T : unmanaged
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

        throw new NotSupportedException(
            $"DrawUserPrimitives<{typeof(T).Name}> is not supported -- only VertexPositionColor, " +
            "VertexPositionColorTexture, VertexPositionTexture, and VertexPositionNormalTexture match a real " +
            "CNA_USER_VERTEX_SOURCE_* identity.");
    }
}
