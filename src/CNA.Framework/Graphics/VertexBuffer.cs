using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed GPU vertex buffer, now matching the real, shipped openeggbert/cna C API
/// (<c>vertex_resources.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s
/// native-ABI-migration entry, step 4.
///
/// Both the <see cref="VertexDeclaration"/>-taking and real XNA's <c>Type</c>-taking constructors
/// are implemented -- the latter (<see cref="VertexBuffer(GraphicsDevice,Type,int,BufferUsage)"/>)
/// is convenience sugar over the former via <see cref="VertexDeclaration.FromType"/>'s reflection,
/// not a second native call.
///
/// <see cref="GetData{T}(T[])"/> always throws -- confirmed directly with <c>cnabinding</c> rather
/// than assumed: the real ABI has no raw-bytes vertex readback route at all (only a typed transfer
/// for the 7 built-in <c>CNA_VertexType</c> values), and CNA's own C++ <c>VertexBuffer</c> has no
/// generic <c>GetData&lt;T&gt;()</c> either -- 14 concrete typed overloads and nothing else. This
/// is not a gap the C binding introduced; a fix would need to start in CNA's C++
/// <c>VertexBuffer</c>, not here. <see cref="SetData{T}(int,T[],int,int,int)"/> is more fortunate --
/// <c>cna_vertex_buffer_set_data_raw</c> is a real, confirmed raw-upload route -- but it has no
/// offset parameter at all (always writes starting at native vertex zero), so a nonzero
/// <c>offsetInBytes</c>-equivalent throws too.
/// </summary>
public class VertexBuffer : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public VertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)
    {
    }

    public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, dynamic: false)
    {
    }

    /// <summary>The one real constructor. <paramref name="dynamic"/> maps straight onto
    /// <c>CNA_VertexBufferCreateInfo.dynamic</c>: a dynamic vertex buffer is the *same* native
    /// resource with that flag set, not a separate type, which is why
    /// <see cref="DynamicVertexBuffer"/> is a thin subclass rather than its own binding.
    /// <c>protected internal</c> so that subclass can reach it without the flag becoming public
    /// API real XNA never had.</summary>
    protected internal VertexBuffer(
        GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage, bool dynamic)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);

        VertexDeclaration = vertexDeclaration;
        VertexCount = vertexCount;
        BufferUsage = bufferUsage;

        CnaHandle declarationHandle = vertexDeclaration.CreateNativeHandle();
        try
        {
            var createInfo = new CnaVertexBufferCreateInfo
            {
                VertexDeclaration = declarationHandle,
                VertexCount = vertexCount,
                BufferUsage = (uint)bufferUsage,
                Dynamic = (byte)(dynamic ? 1 : 0),
            };

            CnaResult result = Native.cna_vertex_buffer_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
            CnaException.ThrowIfFailed(result, nameof(VertexBuffer));

            _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_vertex_buffer_destroy(new CnaHandle(h)));
        }
        finally
        {
            // "Declaration copied into the buffer" (vertex_resources.h:42) -- the native vertex
            // buffer keeps its own copy, so this declaration is never needed again after the call
            // above, whether it succeeded or failed.
            Native.cna_vertex_declaration_destroy(declarationHandle);
        }
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

        if (offsetInBytes != 0)
        {
            throw new NotSupportedException(
                $"{nameof(VertexBuffer)}.{nameof(SetData)} with a nonzero {nameof(offsetInBytes)} is not supported by the real " +
                "cna C API -- cna_vertex_buffer_set_data_raw has no offset parameter at all and always uploads starting at " +
                "native vertex zero.");
        }

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            CnaResult result = Native.cna_vertex_buffer_set_data_raw(
                new CnaHandle(NativeHandleValue), bytePtr, (ulong)((long)elementCount * sizeof(T)), (ulong)elementCount, (uint)vertexStride);
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

    /// <summary>Always throws <see cref="NotSupportedException"/> -- see this class's own doc
    /// comment for why (confirmed with <c>cnabinding</c>, not assumed: CNA's own C++
    /// <c>VertexBuffer</c> has no generic readback for this migration to expose).</summary>
    public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        throw new NotSupportedException(
            $"{nameof(VertexBuffer)}.{nameof(GetData)} is not supported by the real cna C API -- no raw-bytes vertex " +
            "readback route exists (CNA's own C++ VertexBuffer has no generic GetData<T>() either, only 14 concrete " +
            "typed overloads for its 7 built-in vertex types).");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>The overridable half of disposal, so <see cref="DynamicVertexBuffer"/> can release its
    /// <c>ContentLost</c> subscription before the buffer handle that subscription is registered
    /// against goes away.</summary>
    protected virtual void Dispose(bool disposing) => _handle.Dispose();
}
