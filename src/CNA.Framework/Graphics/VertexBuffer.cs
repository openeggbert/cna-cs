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
/// Transfers reach the ABI through three routes and this type picks between them: the typed route
/// for a built-in <c>CNA_VertexType</c> laid out contiguously from vertex zero, the raw route for
/// anything with an offset or a custom stride, and a read-modify-write for XNA's strided
/// scatter/gather, which the ABI has no descriptor for.
///
/// Each of those replaced a refusal, and each refusal had described the ABI rather than the caller:
/// "no raw-bytes vertex readback" (the route had been added), "a nonzero offsetInBytes throws" (so
/// had that one), "SetDataOptions only on the typed upload" (CNA 0.19.0 carries it on the raw
/// upload too, with a documented cost-only deviation on a windowed <c>NoOverwrite</c>), and
/// "cannot represent that scatter update" -- which is true of the ABI and not of the operation:
/// reading the affected window, patching it and writing it back preserves the gaps exactly as XNA
/// does. See <c>ScatterIntoBuffer</c> for what that costs.
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

        System.Runtime.InteropServices.GCHandle pinned =
            System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            if (options != 0 && canUseOptionedTypedRoute)
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
                UploadRaw(offsetInBytes, bytePtr, (ulong)copiedByteCount, nativeVertexCount, nativeStride, options);
                return;
            }

            nativeStride = vertexStride;
            bool contiguousDeclarationWindow = elementSize == nativeStride
                && nativeStride == VertexDeclaration.VertexStride
                && offsetInBytes % nativeStride == 0;
            if (contiguousDeclarationWindow)
            {
                nativeVertexCount = (ulong)elementCount;
                UploadRaw(
                    offsetInBytes, bytePtr, (ulong)((long)elementCount * elementSize),
                    nativeVertexCount, nativeStride, options);
                return;
            }

            ScatterIntoBuffer(bytePtr, elementSize, elementCount, offsetInBytes, vertexStride, options);
        }
        finally
        {
            pinned.Free();
        }
    }

    /// <summary>
    /// XNA's strided update, where <c>vertexStride</c> is the spacing between
    /// <c>sizeof(T)</c>-byte writes and the bytes between them are preserved.
    ///
    /// The ABI has no scatter descriptor: its raw upload writes whole vertices. This refused the
    /// call outright, which rules out every partial-field update a game does -- rewriting only the
    /// positions of an instance stream, only the colours of a particle batch -- and those are
    /// ordinary.
    ///
    /// So the update is composed instead: read the declaration-aligned window that the writes fall
    /// inside, patch the caller's bytes into it at their strides, and write the whole window back.
    /// The gaps are preserved because they are read and written unchanged, which is the guarantee
    /// XNA gives. What this costs is a read, and the read is the honest part of the trade: a
    /// buffer the renderer will not read back cannot use this path, and says so through the native
    /// failure rather than through a guess here.
    /// </summary>
    private unsafe void ScatterIntoBuffer(
        byte* source, int elementSize, int elementCount, int offsetInBytes, int vertexStride, uint options)
    {
        int declarationStride = VertexDeclaration.VertexStride;
        long firstByte = offsetInBytes;
        long lastByteExclusive = (long)offsetInBytes + ((long)(elementCount - 1) * vertexStride) + elementSize;
        long windowStart = firstByte - (firstByte % declarationStride);
        long windowEnd = ((lastByteExclusive + declarationStride - 1) / declarationStride) * declarationStride;
        long windowBytes = windowEnd - windowStart;
        ulong windowVertices = (ulong)(windowBytes / declarationStride);

        var staging = new byte[windowBytes];
        fixed (byte* stagingPtr = staging)
        {
            CnaResult read = Native.cna_vertex_buffer_get_data_raw(
                new CnaHandle(NativeHandleValue), (ulong)windowStart, stagingPtr,
                (ulong)windowBytes, windowVertices, (uint)declarationStride);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(read, nameof(SetData));

            for (int i = 0; i < elementCount; i++)
            {
                long into = ((long)offsetInBytes + ((long)i * vertexStride)) - windowStart;
                Buffer.MemoryCopy(source + ((long)i * elementSize), stagingPtr + into, elementSize, elementSize);
            }

            UploadRaw((int)windowStart, stagingPtr, (ulong)windowBytes, windowVertices, declarationStride, options);
        }
    }

    /// <summary>
    /// The readback half of <see cref="ScatterIntoBuffer"/>: <c>sizeof(T)</c> bytes at each stride,
    /// skipping the gaps. Composed the same way, by reading the aligned window whole and extracting
    /// from it.
    /// </summary>
    private unsafe void GatherFromBuffer(
        byte* destination, int elementSize, int elementCount, int offsetInBytes, int vertexStride)
    {
        int declarationStride = VertexDeclaration.VertexStride;
        long firstByte = offsetInBytes;
        long lastByteExclusive = (long)offsetInBytes + ((long)(elementCount - 1) * vertexStride) + elementSize;
        long windowStart = firstByte - (firstByte % declarationStride);
        long windowEnd = ((lastByteExclusive + declarationStride - 1) / declarationStride) * declarationStride;
        long windowBytes = windowEnd - windowStart;
        ulong windowVertices = (ulong)(windowBytes / declarationStride);

        var staging = new byte[windowBytes];
        fixed (byte* stagingPtr = staging)
        {
            CnaResult read = Native.cna_vertex_buffer_get_data_raw(
                new CnaHandle(NativeHandleValue), (ulong)windowStart, stagingPtr,
                (ulong)windowBytes, windowVertices, (uint)declarationStride);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(read, nameof(GetData));

            // Only after the native call succeeded, so a failure leaves the caller's array alone.
            for (int i = 0; i < elementCount; i++)
            {
                long from = ((long)offsetInBytes + ((long)i * vertexStride)) - windowStart;
                Buffer.MemoryCopy(stagingPtr + from, destination + ((long)i * elementSize), elementSize, elementSize);
            }
        }
    }

    /// <summary>
    /// The raw upload, in the four shapes the ABI offers: window or whole buffer, with or without a
    /// streaming hint.
    ///
    /// The optioned pair arrived in CNA 0.19.0. Before it, a non-<c>None</c> option on any raw or
    /// custom-stride upload was refused outright, because forwarding a route that silently dropped
    /// <c>Discard</c>/<c>NoOverwrite</c> would have been worse than saying so. Both are now
    /// forwarded, and the unoptioned route is still used for <c>None</c> so the common path keeps
    /// the shape it always had.
    /// </summary>
    private unsafe void UploadRaw(
        int offsetInBytes,
        void* data,
        ulong dataByteCount,
        ulong vertexCount,
        int vertexStride,
        uint options)
    {
        // A nonzero offset uses the ABI's buffer-window route; zero replaces from vertex zero.
        CnaResult result = (offsetInBytes == 0, options == 0) switch
        {
            (true, true) => Native.cna_vertex_buffer_set_data_raw(
                new CnaHandle(NativeHandleValue), (byte*)data, dataByteCount, vertexCount, (uint)vertexStride),
            (true, false) => Native.cna_vertex_buffer_set_data_raw_with_options(
                new CnaHandle(NativeHandleValue), data, dataByteCount, vertexCount, (uint)vertexStride, options),
            (false, true) => Native.cna_vertex_buffer_set_data_raw_at(
                new CnaHandle(NativeHandleValue), (ulong)offsetInBytes, data, dataByteCount,
                vertexCount, (uint)vertexStride),
            (false, false) => Native.cna_vertex_buffer_set_data_raw_at_with_options(
                new CnaHandle(NativeHandleValue), (ulong)offsetInBytes, data, dataByteCount,
                vertexCount, (uint)vertexStride, options),
        };

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
                    System.Runtime.InteropServices.GCHandle gathering =
                        System.Runtime.InteropServices.GCHandle.Alloc(
                            data, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        GatherFromBuffer(
                            (byte*)gathering.AddrOfPinnedObject() + ((long)startIndex * elementSize),
                            elementSize, elementCount, offsetInBytes, vertexStride);
                    }
                    finally
                    {
                        gathering.Free();
                    }

                    return;
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
