using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// Matches real XNA's <c>ContentReader</c>: the typed reader a <see cref="ContentTypeReader"/> uses
/// to pull values off a content stream.
///
/// Real XNA derives this from <see cref="System.IO.BinaryReader"/>, so primitive reads
/// (<c>ReadInt32</c>, <c>ReadSingle</c>, <c>ReadString</c>, …) come from the BCL and only the
/// XNA-specific value types get their own methods. That is reproduced here: the primitives are
/// inherited, and only the math/graphics types below cross the ABI, which is exactly the split the
/// C API makes too (it offers one read function per XNA value type and none for the primitives).
/// </summary>
public class ContentReader : BinaryReader
{
    private readonly NativeResourceHandle _handle;

    internal ContentReader(Stream stream, nint nativeHandleValue, ContentManager contentManager, string assetName)
        : base(stream)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_content_reader_destroy(new CnaHandle(h)).IsSuccess());
        _contentManager = contentManager;
        _assetName = assetName;
    }

    /// <summary>
    /// A reader over a handle native owns and will destroy itself -- the shape a
    /// <see cref="ManagedContentTypeReader"/> is handed inside a load callback.
    ///
    /// The distinction is the whole reason this is a separate factory rather than a flag on the
    /// constructor above: that one wraps the handle in an owning
    /// <see cref="NativeResourceHandle"/>, so a reader built through it would run
    /// <c>cna_content_reader_destroy</c> on native's own reader when the callback returned, part
    /// way through the very asset it was reading.
    ///
    /// <see cref="ContentManager"/> and <see cref="AssetName"/> are unavailable here: the callback
    /// receives neither, and <c>cna_content_reader_get_content_manager</c> answers a handle this
    /// binding cannot map back to the managed wrapper that owns it. Both throw rather than
    /// reporting a plausible wrong answer.
    /// </summary>
    internal static ContentReader Borrowing(Stream stream, nint nativeHandleValue) =>
        new(stream, nativeHandleValue);

    private ContentReader(Stream stream, nint nativeHandleValue)
        : base(stream)
    {
        // ownsHandle: false, so the release is never invoked -- native destroys its own reader.
        _handle = new NativeResourceHandle(nativeHandleValue, static _ => true, ownsHandle: false);
    }

    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    private readonly ContentManager? _contentManager;
    private readonly string? _assetName;

    /// <summary>Unavailable on a reader native handed to a
    /// <see cref="ManagedContentTypeReader"/>: the callback receives no managed manager, and
    /// <c>cna_content_reader_get_content_manager</c> answers a handle this binding cannot map back
    /// to the wrapper that owns it. Throws rather than reporting a plausible wrong answer.</summary>
    public ContentManager ContentManager =>
        _contentManager ?? throw new NotSupportedException(
            "This ContentReader was handed to a ManagedContentTypeReader by native, which does not " +
            "pass the managed ContentManager. cna_content_reader_get_content_manager answers a raw " +
            "handle that cannot be mapped back to its managed wrapper.");

    /// <summary>Unavailable on a borrowed reader, for the reason
    /// <see cref="ContentManager"/> gives.</summary>
    public string AssetName =>
        _assetName ?? throw new NotSupportedException(
            "This ContentReader was handed to a ManagedContentTypeReader by native, which does not " +
            "pass the asset name. Read it from cna_content_reader_copy_asset_name if you need it.");

    /// <summary>The content-format version the asset was written with.</summary>
    public int Version
    {
        get
        {
            CnaResult result = Native.cna_content_reader_get_version(NativeHandle, out int value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Version));
            return value;
        }
    }

    /// <summary>The platform byte from the asset header. Left as a raw <see cref="byte"/> rather
    /// than an enum: the C API reports it as one, and the platform identifiers are an open set the
    /// content pipeline defines, not a fixed enumeration this project could pin.</summary>
    public byte Platform
    {
        get
        {
            CnaResult result = Native.cna_content_reader_get_platform(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Platform));
            return value;
        }
    }

    public Matrix ReadMatrix()
    {
        CnaResult result = Native.cna_content_reader_read_matrix(NativeHandle, out CnaMatrix value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadMatrix));
        return Matrix.FromNative(value);
    }

    public Quaternion ReadQuaternion()
    {
        CnaResult result = Native.cna_content_reader_read_quaternion(NativeHandle, out CnaQuaternion value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadQuaternion));
        return Quaternion.FromNative(value);
    }

    public Vector2 ReadVector2()
    {
        CnaResult result = Native.cna_content_reader_read_vector2(NativeHandle, out CnaVector2 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadVector2));
        return Vector2.FromNative(value);
    }

    public Vector3 ReadVector3()
    {
        CnaResult result = Native.cna_content_reader_read_vector3(NativeHandle, out CnaVector3 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadVector3));
        return Vector3.FromNative(value);
    }

    public Vector4 ReadVector4()
    {
        CnaResult result = Native.cna_content_reader_read_vector4(NativeHandle, out CnaVector4 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadVector4));
        return Vector4.FromNative(value);
    }

    public Color ReadColor()
    {
        CnaResult result = Native.cna_content_reader_read_color(NativeHandle, out CnaColor value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadColor));
        return Color.FromNative(value);
    }

    /// <summary>Reads the tag that introduces the next object in the graph. Returns
    /// <see langword="false"/> when the tag encodes a null -- which is how an <c>.xnb</c> object
    /// graph represents one, so this is a real value rather than a failure.</summary>
    public bool ReadObjectTag()
    {
        CnaResult result = Native.cna_content_reader_read_object_tag(NativeHandle, out byte hasValue);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadObjectTag));
        return hasValue != 0;
    }

    /// <summary>Reads exactly <paramref name="count"/> bytes, failing rather than returning short.
    /// <paramref name="readerName"/> appears in the failure message, which is why the native call
    /// takes it -- a truncated asset is far easier to diagnose when the message names the reader
    /// that hit the end.</summary>
    public unsafe byte[] ReadBytesExact(int count, string readerName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(readerName);

        if (count == 0)
        {
            return [];
        }

        byte[] buffer = new byte[count];
        ulong written = 0;
        fixed (byte* bufferPtr = buffer)
        {
            byte* ptr = bufferPtr;
            CnaResult result = CnaStringMarshal.WithStringView(
                readerName, view => Native.cna_content_reader_read_bytes_exact(NativeHandle, count, view, ptr, (ulong)count, out written));
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(ReadBytesExact));
        }

        return written == (ulong)count ? buffer : buffer[..(int)written];
    }

    protected override void Dispose(bool disposing)
    {
        _handle.Dispose();
        base.Dispose(disposing);
    }
}
