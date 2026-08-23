namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>VertexBuffer</c>. It uses composition: transfer calls and scalar
/// properties forward to one <see cref="CNA.Graphics.VertexBuffer"/>, while this facade preserves
/// XNA's own <see cref="GraphicsResource"/> inheritance and namespace-local
/// <see cref="VertexDeclaration"/> type.
///
/// Device-backed behavior is covered by the native integration and graphics behavior probes; the
/// managed-only unit project intentionally cannot construct a compat <c>GraphicsDevice</c>.
/// </summary>
public class VertexBuffer : GraphicsResource
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CNA.Graphics.VertexBuffer, VertexBuffer>
        FrameworkFacades = new();

    private readonly CNA.Graphics.VertexBuffer _frameworkBuffer;
    private readonly VertexDeclaration _vertexDeclaration;
    internal Func<Exception?>? DisposeHook { get; set; }

    public VertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage usage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, usage)
    {
    }

    public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage)
        : this(graphicsDevice, vertexDeclaration, vertexCount, usage, dynamic: false)
    {
    }

    private protected VertexBuffer(
        GraphicsDevice graphicsDevice,
        VertexDeclaration vertexDeclaration,
        int vertexCount,
        BufferUsage bufferUsage,
        bool dynamic)
        : base(graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _vertexDeclaration = vertexDeclaration;
        _frameworkBuffer = new CNA.Graphics.VertexBuffer(
            graphicsDevice.Framework,
            ToFramework(vertexDeclaration),
            vertexCount,
            (CNA.Graphics.BufferUsage)(int)bufferUsage,
            dynamic);
        FrameworkFacades.Add(_frameworkBuffer, this);
    }

    internal CNA.Graphics.VertexBuffer FrameworkBuffer => _frameworkBuffer;

    internal static VertexBuffer? FromFramework(CNA.Graphics.VertexBuffer? frameworkBuffer) =>
        frameworkBuffer is not null && FrameworkFacades.TryGetValue(frameworkBuffer, out VertexBuffer? facade)
            ? facade
            : null;

    internal nint NativeHandleValue => _frameworkBuffer.NativeHandleValue;

    public VertexDeclaration VertexDeclaration => _vertexDeclaration;

    public BufferUsage BufferUsage => (BufferUsage)(int)_frameworkBuffer.BufferUsage;

    public int VertexCount => _frameworkBuffer.VertexCount;

    public void SetData<T>(T[] data) where T : struct => _frameworkBuffer.SetData(data);

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        _frameworkBuffer.SetData(data, startIndex, elementCount);

    public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
        where T : struct =>
        _frameworkBuffer.SetData(offsetInBytes, data, startIndex, elementCount, vertexStride);

    public void GetData<T>(T[] data) where T : struct => _frameworkBuffer.GetData(data);

    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        _frameworkBuffer.GetData(data, startIndex, elementCount);

    public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
        where T : struct =>
        _frameworkBuffer.GetData(offsetInBytes, data, startIndex, elementCount, vertexStride);

    protected override void Dispose(bool arg0)
    {
        if (IsDisposed)
        {
            return;
        }

        Exception? pending = DisposeHook?.Invoke();
        DisposeHook = null;
        _frameworkBuffer?.Dispose();
        base.Dispose(arg0);
        if (pending is not null)
        {
            throw pending;
        }
    }

    private static CNA.Graphics.VertexDeclaration ToFramework(VertexDeclaration vertexDeclaration)
    {
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        return vertexDeclaration.Framework;
    }
}
