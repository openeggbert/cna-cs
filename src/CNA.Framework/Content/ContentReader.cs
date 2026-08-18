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
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_content_reader_destroy(new CnaHandle(h)));
        ContentManager = contentManager;
        AssetName = assetName;
    }

    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public ContentManager ContentManager { get; }

    public string AssetName { get; }

    /// <summary>The content-format version the asset was written with.</summary>
    public int Version
    {
        get
        {
            CnaResult result = Native.cna_content_reader_get_version(NativeHandle, out int value);
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
            CnaException.ThrowIfFailed(result, nameof(Platform));
            return value;
        }
    }

    public Matrix ReadMatrix()
    {
        CnaResult result = Native.cna_content_reader_read_matrix(NativeHandle, out CnaMatrix value);
        CnaException.ThrowIfFailed(result, nameof(ReadMatrix));
        return Matrix.FromNative(value);
    }

    public Quaternion ReadQuaternion()
    {
        CnaResult result = Native.cna_content_reader_read_quaternion(NativeHandle, out CnaQuaternion value);
        CnaException.ThrowIfFailed(result, nameof(ReadQuaternion));
        return Quaternion.FromNative(value);
    }

    public Vector2 ReadVector2()
    {
        CnaResult result = Native.cna_content_reader_read_vector2(NativeHandle, out CnaVector2 value);
        CnaException.ThrowIfFailed(result, nameof(ReadVector2));
        return Vector2.FromNative(value);
    }

    public Vector3 ReadVector3()
    {
        CnaResult result = Native.cna_content_reader_read_vector3(NativeHandle, out CnaVector3 value);
        CnaException.ThrowIfFailed(result, nameof(ReadVector3));
        return Vector3.FromNative(value);
    }

    public Vector4 ReadVector4()
    {
        CnaResult result = Native.cna_content_reader_read_vector4(NativeHandle, out CnaVector4 value);
        CnaException.ThrowIfFailed(result, nameof(ReadVector4));
        return Vector4.FromNative(value);
    }

    public Color ReadColor()
    {
        CnaResult result = Native.cna_content_reader_read_color(NativeHandle, out CnaColor value);
        CnaException.ThrowIfFailed(result, nameof(ReadColor));
        return Color.FromNative(value);
    }

    /// <summary>Reads the tag that introduces the next object in the graph. Returns
    /// <see langword="false"/> when the tag encodes a null -- which is how an <c>.xnb</c> object
    /// graph represents one, so this is a real value rather than a failure.</summary>
    public bool ReadObjectTag()
    {
        CnaResult result = Native.cna_content_reader_read_object_tag(NativeHandle, out byte hasValue);
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
