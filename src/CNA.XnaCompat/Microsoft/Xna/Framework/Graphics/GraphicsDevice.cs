namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>GraphicsDevice</c>. Its native implementation is private so the public
/// XNA device no longer inherits CNA's members, interfaces, or base type.
/// </summary>
public class GraphicsDevice : IDisposable
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        CNA.Graphics.GraphicsDevice,
        GraphicsDevice> FrameworkFacades = new();

    private readonly CNA.Graphics.GraphicsDevice _framework;
    private bool _disposed;

    internal GraphicsDevice(nint nativeGameHandleValue)
        : this(CNA.Graphics.GraphicsDevice.CreateFacadeBackend(nativeGameHandleValue))
    {
    }

    /// <summary>
    /// XNA's public device constructor. CNA's backend creates devices as part of a live game, so
    /// an adapter obtained from a device is adopted rather than creating a second unmanaged device
    /// for the same native game.
    /// </summary>
    public GraphicsDevice(
        GraphicsAdapter adapter,
        GraphicsProfile graphicsProfile,
        PresentationParameters presentationParameters)
        : this((adapter ?? throw new ArgumentNullException(nameof(adapter))).Framework.OwningGraphicsDevice
            ?? throw new NotSupportedException(
                "CNA can construct a GraphicsDevice only from an adapter associated with a live game."))
    {
        ArgumentNullException.ThrowIfNull(presentationParameters);
        _ = graphicsProfile;
        _framework.PresentationParameters = presentationParameters.Framework;
    }

    private GraphicsDevice(CNA.Graphics.GraphicsDevice framework)
    {
        ArgumentNullException.ThrowIfNull(framework);
        _framework = framework;
        FrameworkFacades.Add(_framework, this);
    }

    internal CNA.Graphics.GraphicsDevice Framework => _framework;

    internal static GraphicsDevice? FromFramework(CNA.Graphics.GraphicsDevice? framework) =>
        framework is not null && FrameworkFacades.TryGetValue(framework, out GraphicsDevice? facade)
            ? facade
            : null;

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
    internal bool SupportsCnaCapabilityCore(CNA.XnaCompat.Extensions.CnaGraphicsCapability capability) =>
        _framework.SupportsCapability((CNA.Graphics.GraphicsCapability)(uint)capability);

    internal string CnaRendererName => _framework.RendererName;

    public IndexBuffer? Indices
    {
        get => IndexBuffer.FromFramework(_framework.Indices);
        set => _framework.Indices = value?.FrameworkBuffer;
    }

    public void SetVertexBuffer(VertexBuffer? vertexBuffer) =>
        _framework.SetVertexBuffer(vertexBuffer?.FrameworkBuffer);

    public void SetVertexBuffer(VertexBuffer? vertexBuffer, int vertexOffset)
    {
        if (vertexBuffer is null)
        {
            _framework.SetVertexBuffer(null);
            return;
        }

        _framework.SetVertexBuffers(new CNA.Graphics.VertexBufferBinding(
            vertexBuffer.FrameworkBuffer,
            vertexOffset));
    }

    public void SetRenderTarget(RenderTarget2D? renderTarget) =>
        _framework.SetRenderTarget(renderTarget?.FrameworkTexture as CNA.Graphics.Texture2D);

    public void SetRenderTarget(RenderTargetCube? renderTarget, CubeMapFace cubeMapFace) =>
        _framework.SetRenderTarget(
            renderTarget?.FrameworkTexture as CNA.Graphics.RenderTargetCube,
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace);

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount) =>
        _framework.DrawPrimitives((CNA.Graphics.PrimitiveType)(int)primitiveType, startVertex, primitiveCount);

    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount) =>
        _framework.DrawIndexedPrimitives(
            (CNA.Graphics.PrimitiveType)(int)primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);

    public void Clear(Color color) => _framework.Clear(color.ToFramework());

    public void Clear(ClearOptions options, Color color, float depth, int stencil) =>
        _framework.Clear((CNA.Graphics.ClearOptions)(int)options, color.ToFramework(), depth, stencil);

    public void Clear(ClearOptions options, Vector4 color, float depth, int stencil) =>
        Clear(options, new Color(color.X, color.Y, color.Z, color.W), depth, stencil);

    public Viewport Viewport
    {
        get => Viewport.FromNative(_framework.Viewport);
        set => _framework.Viewport = value.ToNative();
    }

    public GraphicsProfile GraphicsProfile => (GraphicsProfile)_framework.GraphicsProfile;

    public bool IsDisposed => _disposed || _framework.IsDisposed;

    public int MultiSampleMask
    {
        get => _framework.MultiSampleMask;
        set => _framework.MultiSampleMask = value;
    }

    public int ReferenceStencil
    {
        get => _framework.ReferenceStencil;
        set => _framework.ReferenceStencil = value;
    }

    private EventHandler<EventArgs>? _disposing;
    private EventHandler<EventArgs>? _deviceLost;
    private EventHandler<EventArgs>? _deviceReset;
    private EventHandler<EventArgs>? _deviceResetting;
    private EventHandler<ResourceCreatedEventArgs>? _resourceCreated;
    private EventHandler<ResourceDestroyedEventArgs>? _resourceDestroyed;

    public event EventHandler<EventArgs>? Disposing
    {
        add
        {
            if (_disposing is null)
            {
                _framework.Disposing += OnFrameworkDisposing;
            }

            _disposing += value;
        }
        remove
        {
            _disposing -= value;
            if (_disposing is null)
            {
                _framework.Disposing -= OnFrameworkDisposing;
            }
        }
    }

    public event EventHandler<EventArgs>? DeviceLost
    {
        add
        {
            if (_deviceLost is null)
            {
                _framework.DeviceLost += OnFrameworkDeviceLost;
            }

            _deviceLost += value;
        }
        remove
        {
            _deviceLost -= value;
            if (_deviceLost is null)
            {
                _framework.DeviceLost -= OnFrameworkDeviceLost;
            }
        }
    }

    public event EventHandler<EventArgs>? DeviceReset
    {
        add
        {
            if (_deviceReset is null)
            {
                _framework.DeviceReset += OnFrameworkDeviceReset;
            }

            _deviceReset += value;
        }
        remove
        {
            _deviceReset -= value;
            if (_deviceReset is null)
            {
                _framework.DeviceReset -= OnFrameworkDeviceReset;
            }
        }
    }

    public event EventHandler<EventArgs>? DeviceResetting
    {
        add
        {
            if (_deviceResetting is null)
            {
                _framework.DeviceResetting += OnFrameworkDeviceResetting;
            }

            _deviceResetting += value;
        }
        remove
        {
            _deviceResetting -= value;
            if (_deviceResetting is null)
            {
                _framework.DeviceResetting -= OnFrameworkDeviceResetting;
            }
        }
    }

    public event EventHandler<ResourceCreatedEventArgs>? ResourceCreated
    {
        add => _resourceCreated += value;
        remove => _resourceCreated -= value;
    }

    public event EventHandler<ResourceDestroyedEventArgs>? ResourceDestroyed
    {
        add => _resourceDestroyed += value;
        remove => _resourceDestroyed -= value;
    }

    public void Present() => _framework.Present();

    public void Present(Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle) =>
        _framework.Present();

    public void Reset() => _framework.Reset();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool arg0)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DetachBackendEvents();
        if (arg0)
        {
            _framework.Dispose();
        }
    }

    internal void DisposeFromOwningGame()
    {
        if (_disposed)
        {
            return;
        }

        DetachBackendEvents();
        _framework.ReleaseEventSubscriptions();
        _disposed = true;
    }

    private void DetachBackendEvents()
    {
        if (_disposing is not null) _framework.Disposing -= OnFrameworkDisposing;
        if (_deviceLost is not null) _framework.DeviceLost -= OnFrameworkDeviceLost;
        if (_deviceReset is not null) _framework.DeviceReset -= OnFrameworkDeviceReset;
        if (_deviceResetting is not null) _framework.DeviceResetting -= OnFrameworkDeviceResetting;
        _disposing = null;
        _deviceLost = null;
        _deviceReset = null;
        _deviceResetting = null;
        _resourceCreated = null;
        _resourceDestroyed = null;
    }

    private void OnFrameworkDisposing(object? sender, EventArgs args) => _disposing?.Invoke(this, args);

    private void OnFrameworkDeviceLost(object? sender, EventArgs args) => _deviceLost?.Invoke(this, args);

    private void OnFrameworkDeviceReset(object? sender, EventArgs args) => _deviceReset?.Invoke(this, args);

    private void OnFrameworkDeviceResetting(object? sender, EventArgs args) => _deviceResetting?.Invoke(this, args);

    ~GraphicsDevice()
    {
        Dispose(false);
    }

    public void SetVertexBuffers(params VertexBufferBinding[]? vertexBuffers)
    {
        if (vertexBuffers is null)
        {
            _framework.SetVertexBuffers(null);
            return;
        }

        var converted = new CNA.Graphics.VertexBufferBinding[vertexBuffers.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = vertexBuffers[i].ToFramework();
        }

        _framework.SetVertexBuffers(converted);
    }

    /// <summary>Re-typed: <see cref="VertexBufferBinding"/> is a separate value type per namespace,
    /// so the base's array would hand a compat game <c>CNA.Graphics</c> bindings it cannot use.
    /// The base does the cross-check against native's count; this only converts.</summary>
    public VertexBufferBinding[] GetVertexBuffers()
    {
        CNA.Graphics.VertexBufferBinding[] bindings = _framework.GetVertexBuffers();
        var converted = new VertexBufferBinding[bindings.Length];

        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = VertexBufferBinding.FromFramework(bindings[i]);
        }

        return converted;
    }

    public GraphicsAdapter Adapter => GraphicsAdapter.FromFramework(_framework.Adapter);

    public PresentationParameters PresentationParameters
    {
        get => new(_framework.PresentationParameters);
    }

    public DisplayMode DisplayMode => DisplayMode.FromFramework(_framework.DisplayMode);

    /// <summary>Same downcast pass-through pattern as <see cref="Indices"/>, but for a property
    /// whose base default is a real, non-null value. The facade wraps the backend descriptor
    /// instead of relying on an invalid cross-hierarchy cast.</summary>
    public BlendState BlendState
    {
        get => new(_framework.BlendState);
        set => _framework.BlendState = (value ?? throw new ArgumentNullException(nameof(value))).Framework;
    }

    public DepthStencilState DepthStencilState
    {
        get => new(_framework.DepthStencilState);
        set => _framework.DepthStencilState = (value ?? throw new ArgumentNullException(nameof(value))).Framework;
    }

    public RasterizerState RasterizerState
    {
        get => new(_framework.RasterizerState);
        set => _framework.RasterizerState = (value ?? throw new ArgumentNullException(nameof(value))).Framework;
    }

    public SamplerStateCollection SamplerStates => new(this, vertexStage: false);

    public SamplerStateCollection VertexSamplerStates => new(this, vertexStage: true);

    public TextureCollection Textures => new(this, vertexStage: false);

    public TextureCollection VertexTextures => new(this, vertexStage: true);

    /// <summary>Re-typed: <c>Rectangle</c> is a separate struct per namespace. Everything else the
    /// device gained -- <c>Present</c>, <c>Reset</c>, <c>MultiSampleMask</c>,
    /// <c>ReferenceStencil</c>, <c>IsDisposed</c>, <c>DrawInstancedPrimitives</c>, the four
    /// events -- is inherited unchanged, since none of those types diverge.</summary>
    public Rectangle ScissorRectangle
    {
        get => _framework.ScissorRectangle.ToCompat();
        set => _framework.ScissorRectangle = value.ToFramework();
    }

    /// <summary>Re-typed: <c>Color</c> is a separate struct per namespace.</summary>
    public Color BlendFactor
    {
        get => _framework.BlendFactor.ToCompat();
        set => _framework.BlendFactor = value.ToFramework();
    }

    /// <summary>
    /// Copies the back buffer into a caller-provided XNA-compatible pixel buffer. The native CNA
    /// ABI exposes this readback as packed <see cref="Color"/> values, so that representation is
    /// currently the supported generic element type. Keeping the XNA generic overload family is
    /// nevertheless essential: existing XNA callers bind to these signatures, not CNA's former
    /// Color-only convenience members.
    /// </summary>
    public void GetBackBufferData<T>(T[] data)
        where T : struct =>
        GetBackBufferData(null, data, 0, data?.Length ?? 0);

    public void GetBackBufferData<T>(T[] data, int startIndex, int elementCount)
        where T : struct =>
        GetBackBufferData(null, data, startIndex, elementCount);

    public void GetBackBufferData<T>(Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        if (typeof(T) != typeof(Color))
        {
            throw new NotSupportedException(
                "CNA currently exposes back-buffer readback as Microsoft.Xna.Framework.Color values.");
        }

        Color[] colors = (Color[])(object)data;
        var converted = new CNA.Color[colors.Length];
        _framework.GetBackBufferData(
            rect.ToFramework(), converted, startIndex, elementCount);

        for (int i = startIndex; i < startIndex + elementCount; i++)
        {
            colors[i] = converted[i].ToCompat();
        }
    }

    /// <summary>Re-typed: <c>GraphicsDeviceStatus</c> is a separate enum per namespace.</summary>
    public GraphicsDeviceStatus GraphicsDeviceStatus => (GraphicsDeviceStatus)(int)_framework.GraphicsDeviceStatus;

    /// <summary>Re-typed: takes this namespace's own <see cref="PresentationParameters"/>.</summary>
    public void Reset(PresentationParameters presentationParameters)
    {
        ArgumentNullException.ThrowIfNull(presentationParameters);
        _framework.Reset(presentationParameters.Framework);
    }

    /// <summary>Re-typed: takes this namespace's own <see cref="PresentationParameters"/> and
    /// <see cref="GraphicsAdapter"/>.</summary>
    public void Reset(PresentationParameters presentationParameters, GraphicsAdapter graphicsAdapter)
    {
        ArgumentNullException.ThrowIfNull(presentationParameters);
        ArgumentNullException.ThrowIfNull(graphicsAdapter);
        _framework.Reset(presentationParameters.Framework, graphicsAdapter.Framework);
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
        _framework.DrawInstancedPrimitives(
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
            _framework.SetRenderTargets();
            return;
        }

        var converted = new CNA.Graphics.RenderTargetBinding[renderTargets.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = renderTargets[i].Framework;
        }

        _framework.SetRenderTargets(converted);
    }

    public RenderTargetBinding[] GetRenderTargets()
    {
        CNA.Graphics.RenderTargetBinding[] source = _framework.GetRenderTargets();
        var result = new RenderTargetBinding[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = RenderTargetBinding.FromFramework(source[i]);
        }

        return result;
    }

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
    public void DrawUserPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int primitiveCount)
        where T : struct, IVertexType =>
        DrawUserPrimitivesCore(
            primitiveType,
            vertexData,
            vertexOffset,
            primitiveCount,
            TypedVertexSourceFor<T>() is null ? DeclarationFor<T>() : null);

    public void DrawUserPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int primitiveCount,
        VertexDeclaration vertexDeclaration)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        DrawUserPrimitivesCore(
            primitiveType, vertexData, vertexOffset, primitiveCount, vertexDeclaration.Framework);
    }

    /// <summary>Matches real XNA's <c>DrawUserIndexedPrimitives&lt;T&gt;</c>. Reimplements the
    /// vertex-type mapping for the same reason <c>DrawUserPrimitives&lt;T&gt;</c> does -- compat
    /// vertex structs are separate types from their CNA.Graphics counterparts -- and reaches the
    /// shared raw level underneath.</summary>
    public void DrawUserIndexedPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int numVertices,
        int[] indexData,
        int indexOffset,
        int primitiveCount)
        where T : struct, IVertexType =>
        DrawUserIndexedPrimitivesCore(
            primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset,
            primitiveCount, TypedVertexSourceFor<T>() is null ? DeclarationFor<T>() : null);

    public void DrawUserIndexedPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int numVertices,
        short[] indexData,
        int indexOffset,
        int primitiveCount)
        where T : struct, IVertexType =>
        DrawUserIndexedPrimitivesCore(
            primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset,
            primitiveCount, TypedVertexSourceFor<T>() is null ? DeclarationFor<T>() : null);

    public void DrawUserIndexedPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int numVertices,
        int[] indexData,
        int indexOffset,
        int primitiveCount,
        VertexDeclaration vertexDeclaration)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        DrawUserIndexedPrimitivesCore(
            primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset,
            primitiveCount, vertexDeclaration.Framework);
    }

    public void DrawUserIndexedPrimitives<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int numVertices,
        short[] indexData,
        int indexOffset,
        int primitiveCount,
        VertexDeclaration vertexDeclaration)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        DrawUserIndexedPrimitivesCore(
            primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset,
            primitiveCount, vertexDeclaration.Framework);
    }

    private unsafe void DrawUserPrimitivesCore<T>(
        PrimitiveType primitiveType,
        T[] vertexData,
        int vertexOffset,
        int primitiveCount,
        CNA.Graphics.VertexDeclaration? vertexDeclaration)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(vertexData);
        RejectManagedReferences<T>(nameof(vertexData));

        CNA.Graphics.UserVertexSource? typedSource = TypedVertexSourceFor<T>();
        var pin = System.Runtime.InteropServices.GCHandle.Alloc(
            vertexData, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            _framework.DrawUserPrimitivesRaw(
                (CNA.Graphics.PrimitiveType)(int)primitiveType,
                (void*)pin.AddrOfPinnedObject(),
                vertexDeclaration is null && typedSource is { } source
                    ? source
                    : CNA.Graphics.UserVertexSource.RawStream,
                vertexOffset,
                primitiveCount,
                vertexDeclaration);
        }
        finally
        {
            pin.Free();
        }
    }

    private unsafe void DrawUserIndexedPrimitivesCore<TVertex, TIndex>(
        PrimitiveType primitiveType,
        TVertex[] vertexData,
        int vertexOffset,
        int numVertices,
        TIndex[] indexData,
        int indexOffset,
        int primitiveCount,
        CNA.Graphics.VertexDeclaration? vertexDeclaration)
        where TVertex : struct
        where TIndex : struct
    {
        ArgumentNullException.ThrowIfNull(vertexData);
        ArgumentNullException.ThrowIfNull(indexData);
        RejectManagedReferences<TVertex>(nameof(vertexData));
        RejectManagedReferences<TIndex>(nameof(indexData));

        CNA.Graphics.UserVertexSource? typedSource = TypedVertexSourceFor<TVertex>();
        CNA.Graphics.IndexElementSize indexElementSize =
            CNA.Graphics.IndexBuffer.SizeForType(typeof(TIndex));
        var vertexPin = System.Runtime.InteropServices.GCHandle.Alloc(
            vertexData, System.Runtime.InteropServices.GCHandleType.Pinned);
        var indexPin = System.Runtime.InteropServices.GCHandle.Alloc(
            indexData, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            _framework.DrawUserIndexedPrimitivesRaw(
                (CNA.Graphics.PrimitiveType)(int)primitiveType,
                (void*)vertexPin.AddrOfPinnedObject(),
                vertexDeclaration is null && typedSource is { } source
                    ? source
                    : CNA.Graphics.UserVertexSource.RawStream,
                vertexOffset,
                numVertices,
                (void*)indexPin.AddrOfPinnedObject(),
                indexElementSize,
                indexOffset,
                primitiveCount,
                vertexDeclaration);
        }
        finally
        {
            indexPin.Free();
            vertexPin.Free();
        }
    }

    private static void RejectManagedReferences<T>(string parameterName) where T : struct
    {
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new ArgumentException(
                $"Vertex/index type {typeof(T)} contains managed references.", parameterName);
        }
    }

    /// <summary><see langword="null"/> for a vertex type the ABI does not name directly, which is
    /// a fall-through to the raw-stream route rather than a failure -- see
    /// <see cref="CNA.Graphics.UserVertexSource"/> for the header evidence that the raw route was
    /// never actually blocked.</summary>
    private static CNA.Graphics.UserVertexSource? TypedVertexSourceFor<T>() where T : struct
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
    private static CNA.Graphics.VertexDeclaration DeclarationFor<T>() where T : struct
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
