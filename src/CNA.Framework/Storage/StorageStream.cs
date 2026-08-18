using CNA.Interop;

namespace CNA.Storage;

/// <summary>
/// The <see cref="Stream"/> <see cref="StorageContainer.OpenFile"/> and
/// <see cref="StorageContainer.CreateFile"/> hand back, over a native
/// <c>CNA_StorageStreamHandle</c>.
///
/// A real <see cref="Stream"/> subclass rather than a CNA-flavoured stream type, per design
/// invariant #7 -- real XNA returns <see cref="Stream"/> here too, so callers can hand it straight
/// to <see cref="StreamReader"/>, <see cref="System.Xml.Serialization.XmlSerializer"/> or anything
/// else in the BCL.
/// </summary>
internal sealed class StorageStream : Stream
{
    private readonly NativeResourceHandle _handle;

    internal StorageStream(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, ReleaseNative);
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>Closes the stream before destroying it: <c>cna_storage_stream_close</c> is what
    /// flushes and releases the underlying file, while <c>_destroy</c> only frees the handle.
    /// Ordering them the other way would drop buffered writes.</summary>
    private static void ReleaseNative(nint handleValue)
    {
        var handle = new CnaHandle(handleValue);
        Native.cna_storage_stream_close(handle);
    }

    public override bool CanRead => GetFlag(Native.cna_storage_stream_get_can_read, nameof(CanRead));

    public override bool CanWrite => GetFlag(Native.cna_storage_stream_get_can_write, nameof(CanWrite));

    public override bool CanSeek => GetFlag(Native.cna_storage_stream_get_can_seek, nameof(CanSeek));

    public override long Length
    {
        get
        {
            CnaResult result = Native.cna_storage_stream_get_length(NativeHandle, out long value);
            CnaException.ThrowIfFailed(result, nameof(Length));
            return value;
        }
    }

    public override long Position
    {
        get
        {
            CnaResult result = Native.cna_storage_stream_get_position(NativeHandle, out long value);
            CnaException.ThrowIfFailed(result, nameof(Position));
            return value;
        }
        set => Seek(value, SeekOrigin.Begin);
    }

    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

        if (count == 0)
        {
            return 0;
        }

        fixed (byte* bufferPtr = &buffer[offset])
        {
            CnaResult result = Native.cna_storage_stream_read(NativeHandle, bufferPtr, (ulong)count, out ulong read);
            CnaException.ThrowIfFailed(result, nameof(Read));
            return (int)read;
        }
    }

    public override unsafe void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);

        if (count == 0)
        {
            return;
        }

        fixed (byte* bufferPtr = &buffer[offset])
        {
            CnaResult result = Native.cna_storage_stream_write(NativeHandle, bufferPtr, (ulong)count);
            CnaException.ThrowIfFailed(result, nameof(Write));
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        CnaResult result = Native.cna_storage_stream_seek(NativeHandle, offset, (uint)origin, out long position);
        CnaException.ThrowIfFailed(result, nameof(Seek));
        return position;
    }

    public override void SetLength(long value)
    {
        CnaResult result = Native.cna_storage_stream_set_length(NativeHandle, value);
        CnaException.ThrowIfFailed(result, nameof(SetLength));
    }

    public override void Flush()
    {
        CnaResult result = Native.cna_storage_stream_flush(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Flush));
    }

    protected override void Dispose(bool disposing)
    {
        _handle.Dispose();
        base.Dispose(disposing);
    }

    private delegate CnaResult GetFlagFunc(CnaHandle stream, out byte outValue);

    private bool GetFlag(GetFlagFunc getter, string propertyName)
    {
        CnaResult result = getter(NativeHandle, out byte value);
        CnaException.ThrowIfFailed(result, propertyName);
        return value != 0;
    }
}
