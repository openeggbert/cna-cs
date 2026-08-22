namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>IndexBuffer</c>. <c>SetData</c>/<c>GetData</c>/<c>IndexCount</c>/
/// <c>Dispose</c> are inherited unchanged from <see cref="CNA.Graphics.IndexBuffer"/> for the
/// same reason <see cref="VertexBuffer"/>'s equivalent members are. <c>IndexElementSize</c> and
/// <c>BufferUsage</c> need `new` overrides.
/// </summary>
public class IndexBuffer : GraphicsResource
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CNA.Graphics.IndexBuffer, IndexBuffer>
        FrameworkFacades = new();

    private readonly CNA.Graphics.IndexBuffer _frameworkBuffer;
    internal Func<Exception?>? DisposeHook { get; set; }

    /// <summary>Reuses <c>CNA.Graphics.IndexBuffer.SizeForType</c> directly rather than a
    /// compat-namespaced copy -- unlike vertex declarations (which depend on the
    /// namespace-specific <c>IVertexType</c> interface), <c>Type</c>-to-<see cref="IndexElementSize"/>
    /// inference has no compat-specific dependency at all, so there's nothing here for a separate
    /// implementation to differ on.</summary>
    public IndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage usage)
        : this(graphicsDevice, (IndexElementSize)(int)CNA.Graphics.IndexBuffer.SizeForType(indexType), indexCount, usage)
    {
    }

    public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage usage)
        : this(graphicsDevice, indexElementSize, indexCount, usage, dynamic: false)
    {
    }

    private protected IndexBuffer(
        GraphicsDevice graphicsDevice,
        IndexElementSize indexElementSize,
        int indexCount,
        BufferUsage bufferUsage,
        bool dynamic)
        : base(graphicsDevice)
    {
        _frameworkBuffer = new CNA.Graphics.IndexBuffer(
            graphicsDevice,
            (CNA.Graphics.IndexElementSize)(int)indexElementSize,
            indexCount,
            (CNA.Graphics.BufferUsage)(int)bufferUsage,
            dynamic);
        FrameworkFacades.Add(_frameworkBuffer, this);
    }

    internal CNA.Graphics.IndexBuffer FrameworkBuffer => _frameworkBuffer;

    internal static IndexBuffer? FromFramework(CNA.Graphics.IndexBuffer? frameworkBuffer) =>
        frameworkBuffer is not null && FrameworkFacades.TryGetValue(frameworkBuffer, out IndexBuffer? facade)
            ? facade
            : null;

    internal nint NativeHandleValue => _frameworkBuffer.NativeHandleValue;

    public IndexElementSize IndexElementSize => (IndexElementSize)(int)_frameworkBuffer.IndexElementSize;

    public BufferUsage BufferUsage => (BufferUsage)(int)_frameworkBuffer.BufferUsage;

    public int IndexCount => _frameworkBuffer.IndexCount;

    public void SetData<T>(T[] data) where T : struct => _frameworkBuffer.SetData(data);

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        _frameworkBuffer.SetData(0, data, startIndex, elementCount);

    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount)
        where T : struct =>
        _frameworkBuffer.SetData(offsetInBytes, data, startIndex, elementCount);

    public void GetData<T>(T[] data) where T : struct => _frameworkBuffer.GetData(data);

    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        _frameworkBuffer.GetData(0, data, startIndex, elementCount);

    public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount)
        where T : struct =>
        _frameworkBuffer.GetData(offsetInBytes, data, startIndex, elementCount);

    protected override void Dispose(bool arg0)
    {
        if (IsDisposed)
        {
            return;
        }

        Exception? pending = DisposeHook?.Invoke();
        DisposeHook = null;
        _frameworkBuffer.Dispose();
        base.Dispose(arg0);
        if (pending is not null)
        {
            throw pending;
        }
    }
}
