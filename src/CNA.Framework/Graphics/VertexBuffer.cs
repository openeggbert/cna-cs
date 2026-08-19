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

        fixed (T* basePtr = data)
        {
            byte* bytePtr = (byte*)(basePtr + startIndex);
            ulong byteCount = (ulong)((long)elementCount * sizeof(T));

            // A nonzero offsetInBytes threw here until cna_vertex_buffer_set_data_raw_at landed.
            // The plain _raw route has no buffer offset and always starts at native vertex zero, so
            // the two are genuinely different calls rather than one with a defaulted argument.
            CnaResult result = offsetInBytes == 0
                ? Native.cna_vertex_buffer_set_data_raw(
                    new CnaHandle(NativeHandleValue), bytePtr, byteCount, (ulong)elementCount, (uint)vertexStride)
                : Native.cna_vertex_buffer_set_data_raw_at(
                    new CnaHandle(NativeHandleValue), (ulong)offsetInBytes, bytePtr, byteCount,
                    (ulong)elementCount, (uint)vertexStride);

            GC.KeepAlive(this);
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
    /// Two routes, chosen by <typeparamref name="T"/>. A built-in <c>CNA_VertexType</c> layout
    /// reads through the typed route; anything else reads through
    /// <c>cna_vertex_buffer_get_data_raw</c>, which takes a buffer-side byte offset and an explicit
    /// stride.
    ///
    /// Both used to throw -- one because "the C API's typed readback always begins at native vertex
    /// zero", the other because "the C API has no raw-bytes vertex readback". The first is still
    /// true of the typed route and is why the raw route is used whenever an offset or a custom
    /// stride is asked for; the second stopped being true, and the header says why it was closed:
    /// a buffer written through the raw route could never be read back, and "that asymmetry had no
    /// reason behind it".
    /// </summary>
    public unsafe void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(vertexStride, 0);

        // The typed route reads whole vertices of a built-in type from native vertex zero, with no
        // stride override -- so anything that needs an offset, a different stride, or a layout the
        // built-in set does not name goes through the raw route instead.
        if (offsetInBytes != 0 || vertexStride != VertexDeclaration.VertexStride || !HasBuiltInVertexType<T>())
        {
            fixed (T* basePtr = data)
            {
                byte* destination = (byte*)(basePtr + startIndex);
                CnaResult rawResult = Native.cna_vertex_buffer_get_data_raw(
                    new CnaHandle(NativeHandleValue),
                    (ulong)offsetInBytes,
                    destination,
                    (ulong)((long)elementCount * sizeof(T)),
                    (ulong)elementCount,
                    (uint)vertexStride);

                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(rawResult, nameof(GetData));
            }

            return;
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
    /// <summary>Whether the typed readback route can name <typeparamref name="T"/>. The raw route
    /// handles everything else, so this decides which call runs rather than whether the read is
    /// possible at all.</summary>
    private static bool HasBuiltInVertexType<T>() where T : unmanaged =>
        typeof(T) == typeof(VertexPositionColor)
        || typeof(T) == typeof(VertexPositionColorTexture)
        || typeof(T) == typeof(VertexPositionNormalTexture)
        || typeof(T) == typeof(VertexPositionTexture);

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

        // Unreachable from GetData, which routes a non-built-in type to the raw readback before
        // asking for a tag. Kept as a guard for any future caller of the typed route.
        throw new NotSupportedException(
            $"{typeof(T).Name} is not one of the built-in CNA_VertexType layouts, so the typed transfer " +
            "route cannot name it. Read it through the raw route instead (any nonzero offsetInBytes or " +
            "custom vertexStride already selects that).");
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
