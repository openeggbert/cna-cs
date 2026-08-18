using System.Text;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// The ABI's two-call size-then-copy string pattern, in one place.
///
/// Effect reflection alone needs it eight times (parameter/annotation name and semantic, technique
/// and pass name, parameter and annotation string values), and every one is the identical
/// "ask for the byte count, allocate, copy, decode UTF-8" shape -- see
/// <c>CnaError.GetLastErrorMessage</c> and <c>GameWindow.Title</c>, which each hand-rolled it
/// before this existed.
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
}
