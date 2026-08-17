using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed GPU index buffer, now matching the real, shipped openeggbert/cna C API
/// (<c>index_resources.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s
/// native-ABI-migration entry, step 4. Both the <see cref="IndexElementSize"/>-taking and real
/// XNA's <c>Type</c>-taking constructors are implemented, the latter deriving element size from
/// <c>typeof(short)</c>/<c>typeof(ushort)</c>/<c>typeof(int)</c>/<c>typeof(uint)</c>, same
/// "convenience sugar over the same native call" shape as <see cref="VertexBuffer"/>'s equivalent.
///
/// Unlike <see cref="VertexBuffer"/>, <see cref="SetData{T}(T[])"/>/<see cref="GetData{T}(T[])"/>
/// stay fully generic for any 2- or 4-byte unmanaged <c>T</c> -- confirmed directly with
/// <c>cnabinding</c>: <c>cna_index_buffer_set_data</c>/<c>_get_data</c> only ever select a 16- or
/// 32-bit width, with no built-in-type enumeration the way vertex transfer has, because CNA's own
/// C++ <c>IndexBuffer</c> only ever stores <c>uint16_t</c>/<c>uint32_t</c> elements -- a width
/// selector really is the whole story for an index type, not a narrowing the way it is for
/// vertices. Neither real function has an offset parameter into the *native* buffer, though (only
/// a window into the *caller's* own array) -- always operating on native index zero -- so, same as
/// <see cref="VertexBuffer.SetData{T}(int,T[],int,int,int)"/>, a nonzero
/// <paramref name="offsetInBytes"/>-equivalent throws.
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

        var createInfo = new CnaIndexBufferCreateInfo
        {
            IndexCount = indexCount,
            IndexElementSize = (uint)indexElementSize,
            BufferUsage = (uint)bufferUsage,
            Dynamic = 0,
        };

        CnaResult result = Native.cna_index_buffer_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(IndexBuffer));

        _handle = new NativeResourceHandle(handle.Value, h => Native.cna_index_buffer_destroy(new CnaHandle(h)));
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

    /// <summary>Maps an unmanaged element type's own byte size to the real ABI's
    /// <c>CNA_IndexElementSize</c> selector -- unlike <see cref="SizeForType"/> (which maps a
    /// specific <see cref="Type"/> at buffer-construction time), this drives
    /// <see cref="SetData{T}(T[])"/>/<see cref="GetData{T}(T[])"/>'s generic <c>T</c>, so it works
    /// for any 2- or 4-byte unmanaged type, not just the four real XNA names
    /// <see cref="SizeForType"/> recognizes -- confirmed with <c>cnabinding</c> that this stays
    /// correct generically since the real native call only ever inspects the selected width.
    /// </summary>
    private static unsafe uint IndexElementSizeForType<T>() where T : unmanaged => sizeof(T) switch
    {
        2 => (uint)IndexElementSize.SixteenBits,
        4 => (uint)IndexElementSize.ThirtyTwoBits,
        _ => throw new ArgumentException(
            $"Index element type {typeof(T)} must be 2 or 4 bytes (was {sizeof(T)}) -- the real cna C API only stores " +
            "16-bit or 32-bit index elements.", nameof(T)),
    };

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

        if (offsetInBytes != 0)
        {
            throw new NotSupportedException(
                $"{nameof(IndexBuffer)}.{nameof(SetData)} with a nonzero {nameof(offsetInBytes)} is not supported by the real " +
                "cna C API -- cna_index_buffer_set_data has no native-buffer offset parameter at all and always writes " +
                "starting at native index zero.");
        }

        var transfer = new CnaIndexBufferTransfer
        {
            IndexElementSize = IndexElementSizeForType<T>(),
            Options = 0,
            StartIndex = (ulong)startIndex,
            ElementCount = (ulong)elementCount,
        };

        fixed (T* basePtr = data)
        {
            CnaResult result = Native.cna_index_buffer_set_data(
                new CnaHandle(NativeHandleValue), in transfer, (byte*)basePtr, (ulong)elementCount);
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

        if (offsetInBytes != 0)
        {
            throw new NotSupportedException(
                $"{nameof(IndexBuffer)}.{nameof(GetData)} with a nonzero {nameof(offsetInBytes)} is not supported by the real " +
                "cna C API -- cna_index_buffer_get_data has no native-buffer offset parameter at all and always reads " +
                "starting at native index zero.");
        }

        var transfer = new CnaIndexBufferTransfer
        {
            IndexElementSize = IndexElementSizeForType<T>(),
            Options = 0,
            StartIndex = (ulong)startIndex,
            ElementCount = (ulong)elementCount,
        };

        fixed (T* basePtr = data)
        {
            CnaResult result = Native.cna_index_buffer_get_data(
                new CnaHandle(NativeHandleValue), in transfer, (byte*)basePtr, (ulong)elementCount, out _);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
