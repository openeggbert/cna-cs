using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_StringView</c> exactly
/// (<c>core.h:80-86</c>): a borrowed, non-null-terminated UTF-8 byte view, valid only for the
/// duration of the call it is passed to -- not the null-terminated <c>string</c> marshaling
/// (<c>LibraryImport(..., StringMarshalling = Utf8)</c>) this project's P/Invoke declarations used
/// everywhere a string crosses the ABI before this real ABI existed. There is no owning/allocating
/// direction: every real function taking a <see cref="CnaStringView"/> only reads from it, and
/// every real function producing string data instead uses the two-call size/copy pattern
/// (<see cref="CnaError"/>'s own doc comment) into a caller-owned buffer -- so this type is only
/// ever a caller-built, `fixed`-pinned view over a UTF-8-encoded managed buffer, never something
/// this project allocates or owns on its own.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct CnaStringView
{
    public readonly byte* Data;
    public readonly ulong ByteLength;

    public CnaStringView(byte* data, ulong byteLength)
    {
        Data = data;
        ByteLength = byteLength;
    }
}
