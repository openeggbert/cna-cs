using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed GPU index buffer. Same ABI-grounding situation as <see cref="VertexBuffer"/> --
/// no doc-backed shape, but shaped to match the real openeggbert/cna C++ engine's own (not yet
/// C-ABI-exposed) <c>IndexBuffer</c> implementation. Both the <see cref="IndexElementSize"/>-taking
/// and real XNA's <c>Type</c>-taking constructors are implemented, the latter deriving element
/// size from <c>typeof(short)</c>/<c>typeof(ushort)</c>/<c>typeof(int)</c>/<c>typeof(uint)</c>, same
/// "convenience sugar over the same native call" shape as <see cref="VertexBuffer"/>'s equivalent.
/// </summary>
public class IndexBuffer : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public IndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, SizeForType(indexType), indexCount, bufferUsage)
    {
    }

    public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

        IndexElementSize = indexElementSize;
        IndexCount = indexCount;
        BufferUsage = bufferUsage;

        CnaResult result = Native.cna_indexbuffer_create(
            graphicsDevice.ResolveNativeDeviceHandle(), (int)indexElementSize, indexCount, (int)bufferUsage, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(IndexBuffer));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_indexbuffer_release(new CnaHandle(h)));
    }

    /// <summary>Matches real XNA/MonoGame's own <c>Type</c>-to-<see cref="IndexElementSize"/>
    /// mapping exactly (message text recalled from memory, not independently verified -- same
    /// flag as <see cref="VertexDeclaration.FromType"/>'s own). <c>internal</c> rather than
    /// <c>private</c> specifically so it's directly testable without needing a real
    /// <c>cna-native</c> to exercise the constructor it feeds.</summary>
    internal static IndexElementSize SizeForType(Type indexType)
    {
        ArgumentNullException.ThrowIfNull(indexType);

        if (indexType == typeof(short) || indexType == typeof(ushort))
        {
            return IndexElementSize.SixteenBits;
        }

        if (indexType == typeof(int) || indexType == typeof(uint))
        {
            return IndexElementSize.ThirtyTwoBits;
        }

        throw new ArgumentOutOfRangeException(
            nameof(indexType), indexType,
            "Index buffers can only be created for types that are sixteen or thirty two bits in length.");
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
        BufferRangeValidation.ValidateRange(data.Length, startIndex, elementCount);

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
        BufferRangeValidation.ValidateRange(data.Length, startIndex, elementCount);

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
