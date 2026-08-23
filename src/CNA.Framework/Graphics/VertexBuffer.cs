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

            _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_vertex_buffer_destroy(new CnaHandle(h)).IsSuccess());
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

    public void SetData<T>(T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, data, 0, data.Length, 0);
    }

    public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        SetData(0, data, startIndex, elementCount, 0);

    public unsafe void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
        => SetDataWithOptions(offsetInBytes, data, startIndex, elementCount, vertexStride, 0, null);

    internal unsafe void SetDataWithOptions<T>(
        int offsetInBytes,
        T[] data,
        int startIndex,
        int elementCount,
        int vertexStride,
        uint options,
        UserVertexSource? compatTypedSource)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        BufferRangeValidation.ValidateRange(data.Length, startIndex, elementCount);
        if (data.Length == 0)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (elementCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elementCount));
        }

        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new ArgumentException($"Vertex element type {typeof(T)} contains managed references.", nameof(data));
        }

        int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        if (vertexStride != 0 && vertexStride < elementSize)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexStride));
        }

        long copiedByteCount = vertexStride == 0
            ? (long)elementCount * elementSize
            : (long)(elementCount - 1) * vertexStride + elementSize;
        long bufferByteCount = (long)VertexCount * VertexDeclaration.VertexStride;
        if ((long)offsetInBytes + copiedByteCount > bufferByteCount)
        {
            throw new InvalidOperationException("The data does not fit in this VertexBuffer.");
        }

        UserVertexSource? typedSource = compatTypedSource ?? BuiltInVertexSourceFor<T>();
        bool canUseOptionedTypedRoute = offsetInBytes == 0
            && (vertexStride == 0 || vertexStride == VertexDeclaration.VertexStride)
            && elementSize == VertexDeclaration.VertexStride
            && typedSource is not null;

        if (options != 0 && !canUseOptionedTypedRoute)
        {
            throw new NotSupportedException(
                "The CNA C ABI carries SetDataOptions only on its built-in typed vertex upload. " +
                "Raw/custom-stride and buffer-offset vertex uploads cannot represent Discard or NoOverwrite.");
        }

        System.Runtime.InteropServices.GCHandle pinned =
            System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            if (options != 0)
            {
                var transfer = new CnaVertexBufferTransfer
                {
                    VertexType = VertexTypeFor(typedSource!.Value),
                    Options = options,
                    StartIndex = (ulong)startIndex,
                    ElementCount = (ulong)elementCount,
                };
                CnaResult typedResult = Native.cna_vertex_buffer_set_data(
                    new CnaHandle(NativeHandleValue), in transfer,
                    (void*)pinned.AddrOfPinnedObject(), (ulong)data.Length);
                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(typedResult, nameof(SetData));
                return;
            }

            byte* bytePtr = (byte*)pinned.AddrOfPinnedObject() + ((long)startIndex * elementSize);
            int nativeStride;
            ulong nativeVertexCount;
            if (vertexStride == 0)
            {
                nativeStride = VertexDeclaration.VertexStride;
                if (copiedByteCount % nativeStride != 0 || offsetInBytes % nativeStride != 0)
                {
                    throw new NotSupportedException(
                        "The CNA raw VertexBuffer ABI can upload only complete declaration-sized vertices; " +
                        "this contiguous generic byte window ends inside a vertex.");
                }

                nativeVertexCount = (ulong)(copiedByteCount / nativeStride);
                UploadRaw(offsetInBytes, bytePtr, (ulong)copiedByteCount, nativeVertexCount, nativeStride);
                return;
            }

            nativeStride = vertexStride;
            if (elementSize != nativeStride)
            {
                throw new NotSupportedException(
                    "XNA copies only sizeof(T) bytes at each vertexStride and preserves the gaps. " +
                    "The CNA raw VertexBuffer ABI writes complete strides and cannot represent that scatter update.");
            }

            if (nativeStride != VertexDeclaration.VertexStride || offsetInBytes % nativeStride != 0)
            {
                throw new NotSupportedException(
                    "The CNA raw VertexBuffer ABI requires a declaration-sized, vertex-aligned window.");
            }

            nativeVertexCount = (ulong)elementCount;
            UploadRaw(
                offsetInBytes, bytePtr, (ulong)((long)elementCount * elementSize),
                nativeVertexCount, nativeStride);
        }
        finally
        {
            pinned.Free();
        }
    }

    private unsafe void UploadRaw(
        int offsetInBytes,
        void* data,
        ulong dataByteCount,
        ulong vertexCount,
        int vertexStride)
    {
        // A nonzero offset uses the ABI's buffer-window route; zero replaces from vertex zero.
        CnaResult result = offsetInBytes == 0
            ? Native.cna_vertex_buffer_set_data_raw(
                new CnaHandle(NativeHandleValue), (byte*)data, dataByteCount, vertexCount, (uint)vertexStride)
            : Native.cna_vertex_buffer_set_data_raw_at(
                new CnaHandle(NativeHandleValue), (ulong)offsetInBytes, data, dataByteCount,
                vertexCount, (uint)vertexStride);

        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetData));
    }

    public void GetData<T>(T[] data) where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, data, 0, data.Length, 0);
    }

    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct =>
        GetData(0, data, startIndex, elementCount, 0);

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
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);
        if (data.Length == 0)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (elementCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elementCount));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(vertexStride);
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new ArgumentException($"Vertex element type {typeof(T)} contains managed references.", nameof(data));
        }

        int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        long copiedByteCount = vertexStride == 0
            ? (long)elementCount * elementSize
            : (long)(elementCount - 1) * vertexStride + elementSize;
        long bufferByteCount = (long)VertexCount * VertexDeclaration.VertexStride;
        if ((long)offsetInBytes + copiedByteCount > bufferByteCount)
        {
            throw new InvalidOperationException("The requested data does not fit in this VertexBuffer.");
        }

        // The typed route reads whole vertices of a built-in type from native vertex zero, with no
        // stride override -- so anything that needs an offset, a different stride, or a layout the
        // built-in set does not name goes through the raw route instead.
        bool contiguousBuiltIn = offsetInBytes == 0
            && (vertexStride == 0 || vertexStride == elementSize)
            && elementSize == VertexDeclaration.VertexStride
            && HasBuiltInVertexType<T>();
        if (!contiguousBuiltIn)
        {
            int nativeStride;
            ulong nativeVertexCount;
            if (vertexStride == 0)
            {
                nativeStride = VertexDeclaration.VertexStride;
                if (copiedByteCount % nativeStride != 0)
                {
                    throw new NotSupportedException(
                        "The CNA raw VertexBuffer ABI can read only complete declaration-sized vertices; " +
                        "this contiguous generic byte window ends inside a vertex.");
                }

                nativeVertexCount = (ulong)(copiedByteCount / nativeStride);
            }
            else
            {
                if (elementSize != vertexStride)
                {
                    throw new NotSupportedException(
                        "XNA reads sizeof(T) bytes at each vertexStride and skips the gaps. " +
                        "The CNA raw VertexBuffer ABI reads complete strides and cannot represent that scatter readback.");
                }

                nativeStride = vertexStride;
                nativeVertexCount = (ulong)elementCount;
            }

            System.Runtime.InteropServices.GCHandle pinned =
                System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                byte* destination = (byte*)pinned.AddrOfPinnedObject() + ((long)startIndex * elementSize);
                CnaResult rawResult = Native.cna_vertex_buffer_get_data_raw(
                    new CnaHandle(NativeHandleValue),
                    (ulong)offsetInBytes,
                    destination,
                    (ulong)copiedByteCount,
                    nativeVertexCount,
                    (uint)nativeStride);

                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(rawResult, nameof(GetData));
            }
            finally
            {
                pinned.Free();
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

        System.Runtime.InteropServices.GCHandle typedPinned =
            System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            CnaResult result = Native.cna_vertex_buffer_get_data(
                new CnaHandle(NativeHandleValue), in transfer,
                (void*)typedPinned.AddrOfPinnedObject(), (ulong)data.Length, out _);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
        finally
        {
            typedPinned.Free();
        }
    }

    /// <summary>Maps <typeparamref name="T"/> to the <c>CNA_VertexType</c> the typed readback
    /// needs. Only the four built-in layouts that are also real XNA vertex types are reachable --
    /// the other three <c>CNA_VertexType</c> values (tangent and skinned variants) are CNAEXT and
    /// have no XNA counterpart to name here.</summary>
    /// <summary>Whether the typed readback route can name <typeparamref name="T"/>. The raw route
    /// handles everything else, so this decides which call runs rather than whether the read is
    /// possible at all.</summary>
    private static bool HasBuiltInVertexType<T>() where T : struct =>
        typeof(T) == typeof(VertexPositionColor)
        || typeof(T) == typeof(VertexPositionColorTexture)
        || typeof(T) == typeof(VertexPositionNormalTexture)
        || typeof(T) == typeof(VertexPositionTexture);

    private static UserVertexSource? BuiltInVertexSourceFor<T>() where T : struct
    {
        if (typeof(T) == typeof(VertexPositionColor))
        {
            return UserVertexSource.PositionColor;
        }

        if (typeof(T) == typeof(VertexPositionColorTexture))
        {
            return UserVertexSource.PositionColorTexture;
        }

        if (typeof(T) == typeof(VertexPositionNormalTexture))
        {
            return UserVertexSource.PositionNormalTexture;
        }

        if (typeof(T) == typeof(VertexPositionTexture))
        {
            return UserVertexSource.PositionTexture;
        }

        return null;
    }

    private static CnaVertexType VertexTypeFor(UserVertexSource source) => source switch
    {
        UserVertexSource.PositionColor => CnaVertexType.PositionColor,
        UserVertexSource.PositionColorTexture => CnaVertexType.PositionColorTexture,
        UserVertexSource.PositionNormalTexture => CnaVertexType.PositionNormalTexture,
        UserVertexSource.PositionTexture => CnaVertexType.PositionTexture,
        _ => throw new NotSupportedException($"{source} is not a built-in typed vertex layout."),
    };

    private static CnaVertexType VertexTypeFor<T>() where T : struct
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
