using System.Text;

namespace CNA.Interop;

/// <summary>
/// Builds a <see cref="CnaStringView"/> over a UTF-8-encoded copy of a managed <see cref="string"/>
/// and invokes a native call with it, matching the real, shipped openeggbert/cna C API's own
/// pointer+length string convention (see <see cref="CnaStringView"/>'s own doc comment) -- replaces
/// the null-terminated <c>string</c> marshaling (<c>LibraryImport(..., StringMarshalling =
/// Utf8)</c>) this project's P/Invoke declarations used everywhere a string crossed the ABI before
/// this real ABI existed. Short strings (asset names, root directories -- everything this project
/// currently passes) stay stack-allocated; anything longer falls back to a heap buffer rather than
/// risking stack exhaustion.
/// </summary>
internal static unsafe class CnaStringMarshal
{
    private const int StackAllocThreshold = 256;

    public static CnaResult WithStringView(string value, Func<CnaStringView, CnaResult> callNative)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(callNative);

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount <= StackAllocThreshold)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(value, buffer);
            fixed (byte* ptr = buffer)
            {
                return callNative(new CnaStringView(byteCount == 0 ? null : ptr, (ulong)byteCount));
            }
        }

        byte[] heapBuffer = new byte[byteCount];
        Encoding.UTF8.GetBytes(value, heapBuffer);
        fixed (byte* ptr = heapBuffer)
        {
            return callNative(new CnaStringView(ptr, (ulong)byteCount));
        }
    }
}
