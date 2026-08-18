using CNA.Interop;

namespace CNA;

/// <summary>
/// The ABI's two-call size-then-copy pattern for *byte* payloads -- album art, album and picture
/// thumbnails, picture image data.
///
/// Identical in shape to <see cref="NativeStringReader"/>, and deliberately a separate type rather
/// than an overload on it: what comes back here is opaque bytes (a PNG/JPEG stream, in practice),
/// not text, and a caller that reached for a "string reader" to get image bytes would be reading
/// the wrong contract.
/// </summary>
internal static class NativeBlobReader
{
    public delegate CnaResult SizeFunc(CnaHandle handle, out ulong outByteCount);

    public unsafe delegate CnaResult CopyFunc(CnaHandle handle, byte* destination, ulong capacity, out ulong outByteCount);

    /// <summary>Reads the whole payload, or <see langword="null"/> when there is none. Null and
    /// empty are deliberately distinguished: the media types below use "no bytes at all" to mean
    /// "this album has no art", which is a different answer from "the art is a zero-byte
    /// file".</summary>
    public static unsafe byte[]? Read(SizeFunc size, CopyFunc copy, CnaHandle handle, string context)
    {
        CnaResult sizeResult = size(handle, out ulong byteCount);
        CnaException.ThrowIfFailed(sizeResult, context);

        if (byteCount == 0)
        {
            return null;
        }

        byte[] buffer = new byte[byteCount];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = copy(handle, bufferPtr, byteCount, out ulong written);
            CnaException.ThrowIfFailed(copyResult, context);

            // The count can only shrink between the two calls if the payload changed underneath us;
            // returning the whole buffer would then hand back trailing zeros as if they were image
            // data. Trim rather than trust the first answer.
            return written == byteCount ? buffer : buffer[..(int)written];
        }
    }
}
