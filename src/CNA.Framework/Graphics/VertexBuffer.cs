using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed GPU vertex buffer. No ABI shape for this exists anywhere in the analysis docs
/// -- self-designed for this repository, but shaped to match the real openeggbert/cna C++
/// engine's own (not yet C-ABI-exposed) <c>VertexBuffer</c> implementation (a renderer-owned GPU
/// handle plus a CPU-side shadow buffer enabling <see cref="GetData{T}(T[])"/> readback) -- see
/// <see cref="CNA.Interop.Native"/>'s vertex/index buffer section.
///
/// Both the <see cref="VertexDeclaration"/>-taking and real XNA's <c>Type</c>-taking constructors
/// are implemented -- the latter (<see cref="VertexBuffer(GraphicsDevice,Type,int,BufferUsage)"/>)
/// is convenience sugar over the former via <see cref="VertexDeclaration.FromType"/>'s reflection,
/// not a second native call.
/// </summary>
public class VertexBuffer : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public VertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)
    {
    }

    public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);

        VertexDeclaration = vertexDeclaration;
        VertexCount = vertexCount;
        BufferUsage = bufferUsage;

        CnaResult result = Native.cna_vertexbuffer_create(
            graphicsDevice.ResolveNativeDeviceHandle(), vertexDeclaration.VertexStride, vertexCount, (int)bufferUsage, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(VertexBuffer));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_vertexbuffer_release(new CnaHandle(h)));
    }

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    public VertexDeclaration VertexDeclaration { get; }

    public int VertexCount { get; }

    public BufferUsage BufferUsage { get; }

    public void SetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, data, 0, data.Length, VertexDeclaration.VertexStride);
    }

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged =>
        SetData(0, data, startIndex, elementCount, VertexDeclaration.VertexStride);

    public unsafe void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(vertexStride, 0);
        BufferRangeValidation.ValidateRange(data.Length, startIndex, elementCount);

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            CnaResult result = Native.cna_vertexbuffer_set_data(
                new CnaHandle(NativeHandleValue), offsetInBytes, bytePtr, (nuint)((long)elementCount * sizeof(T)), vertexStride);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    public void GetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, data, 0, data.Length, VertexDeclaration.VertexStride);
    }

    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged =>
        GetData(0, data, startIndex, elementCount, VertexDeclaration.VertexStride);

    public unsafe void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(vertexStride, 0);
        BufferRangeValidation.ValidateRange(data.Length, startIndex, elementCount);

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            CnaResult result = Native.cna_vertexbuffer_get_data(
                new CnaHandle(NativeHandleValue), offsetInBytes, bytePtr, (nuint)((long)elementCount * sizeof(T)), vertexStride);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
