using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// A forward-only <see cref="Stream"/> over a native <c>CNA_ContentReaderHandle</c>, built on
/// <c>cna_content_reader_read_bytes_exact</c>.
///
/// This exists to solve a shape mismatch. <see cref="ContentReader"/> derives from
/// <see cref="BinaryReader"/>, so its primitive reads (<c>ReadInt32</c>, <c>ReadString</c>, …) come
/// from the BCL and need a managed stream -- which is right, and matches both XNA and the C API,
/// which offers one read function per XNA value type and none for the primitives. But a reader
/// invoked *from* a native load callback is handed only a native handle: there is no managed stream
/// to hand it, and the bytes it must read are at whatever position native's own stream has reached.
///
/// <c>read_bytes_exact</c> is the bridge. It advances native's position, so a
/// <see cref="BinaryReader"/> layered on this reads exactly the bytes native would next hand out,
/// and the two views of the stream stay in step.
///
/// Read-only, non-seekable, and honest about it: reporting a fake <see cref="Length"/> or a
/// silently-ignored <see cref="Seek"/> would let a reader look like it worked while pulling from
/// the wrong offset.
/// </summary>
internal sealed class NativeContentStream(CnaHandle reader, string readerName) : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException(
        "The native content reader does not report a length. A ContentTypeReader reads forward from " +
        "the current position; it cannot size the stream.");

    public override long Position
    {
        get => throw new NotSupportedException("The native content reader does not expose its position.");
        set => throw new NotSupportedException("The native content reader cannot seek.");
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

        // `_exact` is the whole contract: a short read is a malformed asset, not a partial success,
        // so there is no loop here. Stream.Read is allowed to return fewer bytes than asked, but
        // pretending this route can is what would let a truncated asset deserialize into a
        // plausible-looking object.
        // The pinned pointer cannot be captured by the WithStringView lambda (CS1764), so the
        // string view is materialised first and the fixed block wraps only the call itself.
        CnaResult result = CnaStringMarshal.WithStringView(readerName, view =>
        {
            fixed (byte* destination = &buffer[offset])
            {
                return Native.cna_content_reader_read_bytes_exact(
                    reader, count, view, destination, (ulong)count, out _);
            }
        });

        CnaException.ThrowIfFailed(result, nameof(Read));

        return count;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("The native content reader cannot seek.");

    public override void SetLength(long value) =>
        throw new NotSupportedException("The native content reader is read-only.");

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("The native content reader is read-only.");
}
