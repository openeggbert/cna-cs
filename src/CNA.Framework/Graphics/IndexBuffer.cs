using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed GPU index buffer. Same ABI-grounding situation as <see cref="VertexBuffer"/> --
/// no doc-backed shape, but shaped to match the real openeggbert/cna C++ engine's own (not yet
/// C-ABI-exposed) <c>IndexBuffer</c> implementation. Only the <see cref="IndexElementSize"/>-taking
/// constructor is implemented; real XNA's <c>IndexBuffer(GraphicsDevice, Type, int, BufferUsage)</c>
/// overload (deriving element size from <c>typeof(short)</c>/<c>typeof(int)</c>) was left for a
/// follow-up, same reasoning as <see cref="VertexBuffer"/>'s equivalent omission.
/// </summary>
public class IndexBuffer : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

        IndexElementSize = indexElementSize;
        IndexCount = indexCount;
        BufferUsage = bufferUsage;

        CnaResult result = Native.cna_indexbuffer_create(
            new CnaHandle(graphicsDevice.NativeHandleValue), (int)indexElementSize, indexCount, (int)bufferUsage, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(IndexBuffer));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_indexbuffer_release(new CnaHandle(h)));
    }

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    public IndexElementSize IndexElementSize { get; }

    public int IndexCount { get; }

    public BufferUsage BufferUsage { get; }

    public void SetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, data, 0, data.Length);
    }

    public unsafe void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        if (startIndex > data.Length || elementCount > data.Length - startIndex)
        {
            throw new ArgumentException($"{nameof(startIndex)} + {nameof(elementCount)} exceeds the data array's length.");
        }

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            CnaResult result = Native.cna_indexbuffer_set_data(
                new CnaHandle(NativeHandleValue), offsetInBytes, bytePtr, (nuint)((long)elementCount * sizeof(T)));
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    public void GetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, data, 0, data.Length);
    }

    public unsafe void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        if (startIndex > data.Length || elementCount > data.Length - startIndex)
        {
            throw new ArgumentException($"{nameof(startIndex)} + {nameof(elementCount)} exceeds the data array's length.");
        }

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            CnaResult result = Native.cna_indexbuffer_get_data(
                new CnaHandle(NativeHandleValue), offsetInBytes, bytePtr, (nuint)((long)elementCount * sizeof(T)));
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
