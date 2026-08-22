using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed 2D texture, now matching the real, shipped openeggbert/cna C API
/// (<c>graphics.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s native-ABI-migration
/// entry, step 5. <c>cna_texture2d_create</c> here (<c>graphics.h</c>) is a different, simpler
/// function from the several routes in <c>texture.h</c> -- this is the one that actually allocates
/// an empty, dimensions-only, device-attached texture the way this constructor needs.
///
/// <see cref="ReleaseNative"/>/<see cref="Width"/>/<see cref="Height"/> are overridable so
/// <see cref="RenderTarget2D"/> -- a real, *separate* resource type upstream with its own
/// create/get_info/destroy routes, not a texture created with special usage flags -- can override
/// them to call its own native functions while still sharing the disposal and
/// <c>NativeHandleValue</c> machinery it inherits, matching real XNA's own
/// <c>RenderTarget2D : Texture2D</c> inheritance.
///
/// Phase 8 WP3 reparented this onto <see cref="Texture"/> (and through it
/// <see cref="GraphicsResource"/>), matching real XNA's own
/// <c>Texture2D : Texture : GraphicsResource</c> chain. Handle ownership, disposal, and
/// <c>ReleaseNative</c> all moved up to <see cref="Texture"/>; what stays here is what is genuinely
/// 2D-specific (<see cref="Width"/>/<see cref="Height"/>, which come from <c>graphics.h</c>'s own
/// <c>CNA_Texture2DInfo</c> rather than the dimensionless shared <c>CNA_TextureInfo</c>, and the
/// data-transfer methods).
/// </summary>
public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : this(graphicsDevice, width, height, mipMap: false, SurfaceFormat.Color)
    {
    }

    public Texture2D(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        bool mipMap,
        SurfaceFormat format)
        : base(graphicsDevice, CreateNativeTexture2DHandle(graphicsDevice, width, height, mipMap, format))
    {
    }

    /// <summary>Creates the native texture handle without wrapping it. <c>internal</c> (visible to
    /// CNA.XnaCompat through the assembly's <c>InternalsVisibleTo</c> grant) for exactly the reason
    /// <c>RenderTarget2D.CreateNativeHandle</c> already is: CNA.XnaCompat's own
    /// <c>Texture2D</c> derives from CNA.XnaCompat's <c>Texture</c> -- so that
    /// <c>Texture t = someTexture2D;</c> compiles in game code, as it does in real XNA -- and
    /// therefore cannot inherit this type's implementation, but must still make the identical
    /// native call.</summary>
    internal static nint CreateNativeTexture2DHandle(GraphicsDevice graphicsDevice, int width, int height)
        => CreateNativeTexture2DHandle(graphicsDevice, width, height, mipMap: false, SurfaceFormat.Color);

    internal static nint CreateNativeTexture2DHandle(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        bool mipMap,
        SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        var createInfo = new CnaTexture2DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)format,
        };

        CnaResult result = Native.cna_texture2d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(Texture2D));

        return handle.AsNint;
    }

    /// <summary>
    /// Wraps an already-created native texture handle -- used by <c>ContentManager.Load&lt;T&gt;</c>,
    /// which receives the handle directly from <c>cna_content_manager_load_texture2d</c> rather than
    /// creating a texture from scratch. <c>protected internal</c> so CNA.XnaCompat's
    /// <c>Texture2D</c> subclass constructor can forward to it -- see docs/architecture.md.
    ///
    /// Gained its <paramref name="graphicsDevice"/> parameter in Phase 8 WP3: every
    /// <see cref="GraphicsResource"/> has a non-null <c>GraphicsDevice</c> in real XNA, so the
    /// device can no longer be omitted here. That is why <c>ContentManager.Load&lt;Texture2D&gt;</c>
    /// now requires a device to have been assigned, the same way its <c>Load&lt;Model&gt;</c> path
    /// already did.
    /// </summary>
    protected internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    /// <summary>Wraps a borrowed handle -- see <see cref="Texture"/>'s equivalent
    /// constructor.</summary>
    private protected Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue, bool ownsHandle)
        : base(graphicsDevice, nativeHandleValue, ownsHandle)
    {
    }

    /// <summary>Builds a non-owning wrapper over a texture handle whose real owner is something
    /// else. <c>internal</c> because only this assembly knows which native calls hand out borrowed
    /// handles -- see <c>VideoPlayer.GetTexture</c>.</summary>
    internal static Texture2D CreateBorrowed(GraphicsDevice graphicsDevice, nint nativeHandleValue) =>
        new(graphicsDevice, nativeHandleValue, ownsHandle: false);

    /// <summary>Matches <c>cna_texture2d_destroy</c> exactly (<c>graphics.h:702</c>) -- confirmed
    /// real (an earlier, name-only <c>nm -D</c> pass had guessed this correctly by coincidence, but
    /// a later, more careful pass reading <c>texture.h</c> alone could not find it there and flagged
    /// the claim as unconfirmed; it exists, just in <c>graphics.h</c>, not <c>texture.h</c>).
    /// Overrides <see cref="Texture.ReleaseNative"/> (abstract there -- there is no shared
    /// <c>cna_texture_destroy</c>) and stays overridable itself so <see cref="RenderTarget2D"/> can
    /// release through <c>cna_render_target_destroy</c> instead.</summary>
    protected override void ReleaseNative(nint handleValue) => ReleaseNativeTexture2D(handleValue);

    /// <summary>Sourced from <c>cna_texture2d_get_info</c> (<c>graphics.h</c>'s own
    /// <c>CNA_Texture2DInfo</c>, which reports real width/height) -- not <c>texture.h</c>'s
    /// differently-shaped <c>CNA_TextureInfo</c>, which has no dimensions at all. <see langword="virtual"/>
    /// so <see cref="RenderTarget2D"/> can source its own dimensions from
    /// <c>cna_render_target_get_info</c> instead.</summary>
    public virtual int Width => GetTexture2DDimensions(NativeHandleValue).Width;

    public virtual int Height => GetTexture2DDimensions(NativeHandleValue).Height;

    /// <summary>Returns a plain tuple rather than <see cref="CnaTexture2DInfo"/> so CNA.XnaCompat
    /// can call it without naming a CNA.Interop type -- identical in shape and rationale to
    /// <see cref="RenderTarget2D.GetDimensions"/>. Named for the concrete type (not just
    /// <c>GetDimensions</c>) so it does not collide with <see cref="RenderTarget2D"/>'s own
    /// same-purpose helper, which reads a different native info struct.</summary>
    internal static (int Width, int Height) GetTexture2DDimensions(nint handleValue)
    {
        var info = new CnaTexture2DInfo();
        CnaResult result = Native.cna_texture2d_get_info(new CnaHandle(handleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texture2d_get_info");
        return ((int)info.Width, (int)info.Height);
    }

    internal static void ReleaseNativeTexture2D(nint handleValue) => Native.cna_texture2d_destroy(new CnaHandle(handleValue));

    /// <summary>The shared body of both <c>SetData</c> overloads' native call, reusable by
    /// CNA.XnaCompat's parallel <c>Texture2D</c> -- see <c>CreateNativeTexture2DHandle</c>.</summary>
    internal static unsafe void SetDataRgba8(nint handleValue, ReadOnlySpan<byte> data)
    {
        if (data.Length % 4 != 0)
        {
            throw new ArgumentException("data.Length must be a multiple of 4 (one CNA_Color per pixel, 4 bytes each).", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            CnaResult result = Native.cna_texture2d_set_data_rgba8(
                new CnaHandle(handleValue), (CnaColor*)dataPtr, (ulong)(data.Length / 4));
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    /// <summary>Matches <c>cna_texture2d_set_data_rgba8</c> exactly (<c>graphics.h:674</c>) --
    /// takes a <c>const CNA_Color*</c> pixel array plus a pixel count, not the old guessed shape's
    /// raw byte pointer plus byte length. <paramref name="data"/>'s bytes are reinterpreted as
    /// <see cref="CnaColor"/> elements (4 bytes each, the same RGBA8 byte layout either way) rather
    /// than re-encoding them, since this method's own public contract was already "raw RGBA8
    /// bytes" before this migration -- not a behavior change, just how those same bytes now reach
    /// native.</summary>
    public void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataRgba8(NativeHandleValue, data);
    }

    /// <summary>The whole surface as a rectangle at the origin. Matches real XNA.</summary>
    public Rectangle Bounds => new(0, 0, Width, Height);

    /// <summary>
    /// Matches real XNA's <c>GetData</c>: reads texels back into a caller array.
    ///
    /// <b>This threw until the header was re-read.</b> The message said
    /// <c>cna_texture2d_get_data</c> was unreachable because "this binding has no route to verify
    /// that an arbitrary element type matches that format ... needs a format-and-element-size query
    /// upstream". <c>cna_texture_validate_get_data_format</c> is that query, and it sits in the same
    /// header, a hundred lines from the read it was guarding. Twelfth false "the C API cannot do
    /// this" claim in this repository; see <c>plan.md</c>'s Corrections table.
    ///
    /// The safety it was reaching for is now real, in two parts:
    /// <see cref="TextureDataType.Of{T}"/> refuses an element type the ABI has no overload for,
    /// by name, and the native validation confirms the size divides this texture's surface-format
    /// unit. Both run before a single byte is read.
    /// </summary>
    /// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not one of the
    /// element types <c>CNA_TextureDataType</c> names.</exception>
    /// <exception cref="CnaException">If the element size is incompatible with this texture's
    /// format, or the region is out of range. The read is atomic -- a failure leaves
    /// <paramref name="data"/> untouched.</exception>
    public void GetData<T>(T[] data) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        GetData(0, null, data, 0, data.Length);
    }

    /// <summary>See <see cref="GetData{T}(T[])"/>.</summary>
    public void GetData<T>(T[] data, int startIndex, int elementCount) where T : unmanaged =>
        GetData(0, null, data, startIndex, elementCount);

    /// <summary>
    /// The shared body of every <c>GetData</c> overload's native call, reusable by CNA.XnaCompat's
    /// parallel <c>Texture2D</c> -- the same reason <see cref="SetDataRgba8"/> is internal.
    ///
    /// Compat's <c>Texture2D</c> derives from *its own* <c>Texture</c> rather than from this type,
    /// so it inherits nothing here and every member has to be re-offered explicitly. That is why
    /// <c>GetData</c> existed on this layer for some time and was simply absent from the one a
    /// ported game uses.
    /// </summary>
    internal static unsafe void GetDataInto<T>(
        nint handleValue, SurfaceFormat format, int level, Rectangle? rect,
        T[] data, int startIndex, int elementCount)
        where T : unmanaged =>
        GetDataInto(handleValue, format, TextureDataType.Of<T>(), level, rect, data, startIndex, elementCount);

    internal static unsafe void GetDataInto<T>(
        nint handleValue, SurfaceFormat format, uint dataType, int level, Rectangle? rect,
        T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new ArgumentException($"Texture element type {typeof(T)} contains managed references.", nameof(data));
        }

        CnaResult validation = Native.cna_texture_validate_get_data_format(
            (uint)format, System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
        CnaException.ThrowIfFailed(validation, nameof(GetData));

        CnaTexture2DTransfer transfer = CnaTexture2DTransfer.Versioned();
        transfer.Level = level;
        transfer.StartIndex = (ulong)startIndex;
        transfer.ElementCount = (ulong)elementCount;

        if (rect is { } region)
        {
            transfer.HasRectangle = 1;
            transfer.Rectangle = new CnaRectangle(region.X, region.Y, region.Width, region.Height);
        }

        System.Runtime.InteropServices.GCHandle pinned =
            System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            CnaResult result = Native.cna_texture2d_get_data(
                new CnaHandle(handleValue), dataType, &transfer,
                (void*)pinned.AddrOfPinnedObject(), (ulong)data.Length, out _);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
        finally
        {
            pinned.Free();
        }
    }

    internal static unsafe void SetDataFrom<T>(
        nint handleValue, uint dataType, int level, Rectangle? rect,
        T[] data, int startIndex, int elementCount)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new ArgumentException($"Texture element type {typeof(T)} contains managed references.", nameof(data));
        }

        var transfer = CnaTexture2DTransfer.Versioned();
        transfer.Level = level;
        transfer.StartIndex = (ulong)startIndex;
        transfer.ElementCount = (ulong)elementCount;
        if (rect is { } region)
        {
            transfer.HasRectangle = 1;
            transfer.Rectangle = new CnaRectangle(region.X, region.Y, region.Width, region.Height);
        }

        System.Runtime.InteropServices.GCHandle pinned =
            System.Runtime.InteropServices.GCHandle.Alloc(data, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            CnaResult result = Native.cna_texture2d_set_data(
                new CnaHandle(handleValue), dataType, &transfer,
                (void*)pinned.AddrOfPinnedObject(), (ulong)data.Length);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
        finally
        {
            pinned.Free();
        }
    }

    /// <summary>See <see cref="GetData{T}(T[])"/>.</summary>
    public unsafe void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        uint dataType = TextureDataType.Of<T>();

        // Before the read, not after: the whole point of the check is that reading the wrong width
        // corrupts silently rather than failing.
        CnaResult validation = Native.cna_texture_validate_get_data_format((uint)Format, sizeof(T));
        CnaException.ThrowIfFailed(validation, nameof(GetData));

        CnaTexture2DTransfer transfer = CnaTexture2DTransfer.Versioned();
        transfer.Level = level;
        transfer.StartIndex = (ulong)startIndex;
        transfer.ElementCount = (ulong)elementCount;

        if (rect is { } region)
        {
            transfer.HasRectangle = 1;
            transfer.Rectangle = new CnaRectangle(region.X, region.Y, region.Width, region.Height);
        }

        fixed (T* destination = data)
        {
            CnaResult result = Native.cna_texture2d_get_data(
                new CnaHandle(NativeHandleValue), dataType, &transfer, destination, (ulong)data.Length, out _);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(GetData));
        }
    }

    /// <summary>
    /// Matches real XNA's <c>Texture2D.FromStream</c>: decodes an encoded image (PNG, JPEG, DDS or
    /// whatever else the renderer supports) into a texture.
    ///
    /// The whole stream is read into memory first, because the ABI decodes from a contiguous byte
    /// block rather than from a callback-driven reader. That matches what XNA's own implementation
    /// does with a non-seekable stream anyway.
    ///
    /// Found unbound by a sweep of header functions with no binding --
    /// <c>cna_texture2d_create_from_encoded_memory</c> had been there all along.
    /// </summary>
    public static unsafe Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream) =>
        FromStreamCore(graphicsDevice, stream, null);

    /// <summary>Decodes and fits or cover-crops an image to the requested dimensions through the
    /// native ABI's versioned decode descriptor.</summary>
    public static unsafe Texture2D FromStream(
        GraphicsDevice graphicsDevice,
        Stream stream,
        int width,
        int height,
        bool zoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var decodeInfo = new CnaTexture2DDecodeInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            Zoom = (byte)(zoom ? 1 : 0),
        };
        return FromStreamCore(graphicsDevice, stream, &decodeInfo);
    }

    private static unsafe Texture2D FromStreamCore(
        GraphicsDevice graphicsDevice,
        Stream stream,
        CnaTexture2DDecodeInfo* decodeInfo)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] encoded = buffer.ToArray();

        CnaHandle texture;
        fixed (byte* encodedPtr = encoded)
        {
            // A null decode_info preserves the source dimensions, which is what FromStream means.
            CnaResult result = Native.cna_texture2d_create_from_encoded_memory(
                graphicsDevice.ResolveNativeDeviceHandle(), encodedPtr, (ulong)encoded.Length, decodeInfo, out texture);
            CnaException.ThrowIfFailed(result, nameof(FromStream));
        }

        return new Texture2D(graphicsDevice, texture.AsNint);
    }

    /// <summary>Matches real XNA's <c>SaveAsPng</c>. <paramref name="width"/>/
    /// <paramref name="height"/> are the encoded size, which XNA allows to differ from the
    /// texture's own.</summary>
    public void SaveAsPng(Stream stream, int width, int height) =>
        SaveAs(stream, CnaTextureImageFormat.Png, width, height, nameof(SaveAsPng));

    /// <summary>Matches real XNA's <c>SaveAsJpeg</c>.</summary>
    public void SaveAsJpeg(Stream stream, int width, int height) =>
        SaveAs(stream, CnaTextureImageFormat.Jpeg, width, height, nameof(SaveAsJpeg));

    /// <summary>Encodes and writes. Asks native for the exact byte count first rather than guessing
    /// a buffer size -- the same two-call shape every other sized read in this binding uses, and the
    /// reason a partial or truncated image cannot be written.</summary>
    private unsafe void SaveAs(Stream stream, CnaTextureImageFormat format, int width, int height, string context)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var handle = new CnaHandle(NativeHandleValue);
        CnaResult sizeResult = Native.cna_texture2d_get_encoded_byte_count(
            handle, (uint)format, (uint)width, (uint)height, out ulong byteCount);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(sizeResult, context);

        if (byteCount == 0)
        {
            return;
        }

        byte[] encoded = new byte[byteCount];
        ulong written;
        fixed (byte* encodedPtr = encoded)
        {
            CnaResult copyResult = Native.cna_texture2d_copy_encoded(
                handle, (uint)format, (uint)width, (uint)height, encodedPtr, byteCount, out written);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(copyResult, context);
        }

        stream.Write(encoded, 0, (int)written);
    }

    /// <summary>Convenience overload matching real XNA's common <c>SetData&lt;Color&gt;</c> usage
    /// (the general <c>SetData&lt;T&gt;</c> generic itself isn't implemented -- no caller in this
    /// project needs any other <c>T</c>). Goes straight through
    /// <c>cna_texture2d_set_data_rgba8</c>'s own <c>const CNA_Color*</c> pixel-array shape rather
    /// than routing through <see cref="SetData(byte[])"/>, since a <see cref="Color"/> array is
    /// already exactly that layout.</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataRgba8(NativeHandleValue, PackColors(data));
    }

    /// <summary>Packs a managed <see cref="Color"/> array into the RGBA8 byte layout
    /// <c>cna_texture2d_set_data_rgba8</c> expects. <c>internal</c> so CNA.XnaCompat's parallel
    /// <c>Texture2D</c> can reuse the packing instead of writing its own loop.</summary>
    internal static byte[] PackColors(ReadOnlySpan<Color> data)
    {
        var packed = new byte[data.Length * 4];
        for (int i = 0; i < data.Length; i++)
        {
            packed[(i * 4) + 0] = data[i].R;
            packed[(i * 4) + 1] = data[i].G;
            packed[(i * 4) + 2] = data[i].B;
            packed[(i * 4) + 3] = data[i].A;
        }

        return packed;
    }

}
