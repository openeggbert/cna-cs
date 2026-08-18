using System.Text;
using CNA.Interop;

namespace CNA;

/// <summary>
/// The ABI's two-call size-then-copy string pattern, in one place.
///
/// Effect reflection alone needs it eight times (parameter/annotation name and semantic, technique
/// and pass name, parameter and annotation string values), and every one is the identical
/// "ask for the byte count, allocate, copy, decode UTF-8" shape -- see
/// <c>CnaError.GetLastErrorMessage</c> and <c>GameWindow.Title</c>, which each hand-rolled it
/// before this existed.
///
/// Lives in the root <c>CNA</c> namespace rather than <c>CNA.Graphics</c> where it was first
/// written: the pattern is an ABI-wide convention, not a graphics one, and <c>CNA.Media</c>'s
/// <c>Video</c> needs it too.
/// </summary>
internal static class NativeStringReader
{
    public delegate CnaResult SizeFunc(CnaHandle handle, out ulong outByteCount);

    public unsafe delegate CnaResult CopyFunc(CnaHandle handle, byte* destination, ulong capacity, out ulong outByteCount);

    public static unsafe string Read(SizeFunc size, CopyFunc copy, CnaHandle handle, string context)
    {
        CnaResult sizeResult = size(handle, out ulong byteCount);
        CnaException.ThrowIfFailed(sizeResult, context);

        if (byteCount == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteCount];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = copy(handle, bufferPtr, byteCount, out ulong written);
            CnaException.ThrowIfFailed(copyResult, context);
            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }

    /// <summary>The same pattern for the index-addressed families (microphones, storage container
    /// entries) whose size/copy calls take an extra index alongside the handle.</summary>
    public delegate CnaResult IndexedSizeFunc(CnaHandle handle, ulong index, out ulong outByteCount);

    public unsafe delegate CnaResult IndexedCopyFunc(CnaHandle handle, ulong index, byte* destination, ulong capacity, out ulong outByteCount);

    public static unsafe string ReadIndexed(
        IndexedSizeFunc size, IndexedCopyFunc copy, CnaHandle handle, ulong index, string context)
    {
        CnaResult sizeResult = size(handle, index, out ulong byteCount);
        CnaException.ThrowIfFailed(sizeResult, context);

        if (byteCount == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteCount];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = copy(handle, index, bufferPtr, byteCount, out ulong written);
            CnaException.ThrowIfFailed(copyResult, context);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }

}
