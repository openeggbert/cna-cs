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
/// <see cref="GetData{T}(T[])"/> works for the vertex types the ABI's typed readback names, and
/// throws for the rest. There is genuinely no raw-bytes vertex readback route -- only
/// <c>cna_vertex_buffer_get_data</c> over the seven built-in <c>CNA_VertexType</c> layouts, four of
/// which are real XNA types. It used to throw for every element type, and a header audit found the
/// typed route unbound; the "no readback at all" half of the old note was correct only about raw
/// bytes.
///
/// <see cref="SetData{T}(int,T[],int,int,int)"/> uses <c>cna_vertex_buffer_set_data_raw</c>, a real
/// raw-upload route, but one with no offset parameter at all (it always writes from native vertex
/// zero), so a nonzero <c>offsetInBytes</c> throws. The same asymmetry applies to readback: the
/// header states that native readback begins at vertex zero and <c>start_index</c> selects the
/// *caller's* array window, so a nonzero <c>offsetInBytes</c> throws there too.
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

    /// <summary>
    /// Reads vertices back into <paramref name="data"/>.
    ///
    /// <paramref name="offsetInBytes"/> must be zero and <paramref name="vertexStride"/> must match
    /// the declaration's own: the ABI reads from native vertex zero at the type's natural stride,
    /// and quietly ignoring either argument would return the wrong vertices rather than failing.
    /// </summary>
    public unsafe void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        if (offsetInBytes != 0)
        {
            throw new NotSupportedException(
                $"{nameof(VertexBuffer)}.{nameof(GetData)} cannot start at a nonzero offsetInBytes -- the C API's " +
                "typed readback always begins at native vertex zero (its own start_index selects the caller's array " +
                "window instead).");
        }

        if (vertexStride != VertexDeclaration.VertexStride)
        {
            throw new NotSupportedException(
                $"{nameof(VertexBuffer)}.{nameof(GetData)} cannot read at a vertexStride ({vertexStride}) other than " +
                $"the buffer's own ({VertexDeclaration.VertexStride}) -- the C API reads whole vertices of a built-in " +
                "type, with no stride override.");
        }

        CnaVertexType vertexType = VertexTypeFor<T>();
        var transfer = new CnaVertexBufferTransfer
        {
            VertexType = vertexType,
            Options = 0,
            StartIndex = (ulong)startIndex,
            ElementCount = (ulong)elementCount,
        };

        fixed (T* destination = data)
        {
            CnaResult result = Native.cna_vertex_buffer_get_data(
                new CnaHandle(NativeHandleValue), in transfer, destination, (ulong)data.Length, out _);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
    }

    /// <summary>Maps <typeparamref name="T"/> to the <c>CNA_VertexType</c> the typed readback
    /// needs. Only the four built-in layouts that are also real XNA vertex types are reachable --
    /// the other three <c>CNA_VertexType</c> values (tangent and skinned variants) are CNAEXT and
    /// have no XNA counterpart to name here.</summary>
    private static CnaVertexType VertexTypeFor<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(VertexPositionColor))
        {
            return CnaVertexType.PositionColor;
        }

        if (typeof(T) == typeof(VertexPositionColorTexture))
        {
            return CnaVertexType.PositionColorTexture;
        }

        if (typeof(T) == typeof(VertexPositionNormalTexture))
        {
            return CnaVertexType.PositionNormalTexture;
        }

        if (typeof(T) == typeof(VertexPositionTexture))
        {
            return CnaVertexType.PositionTexture;
        }

        throw new NotSupportedException(
            $"{nameof(VertexBuffer)}.{nameof(GetData)}<{typeof(T).Name}> is not supported -- the C API has no " +
            "raw-bytes vertex readback, only a typed one over its built-in layouts, so only VertexPositionColor, " +
            "VertexPositionColorTexture, VertexPositionNormalTexture and VertexPositionTexture can be read back.");
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
