using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GameComponentCallbacks</c> exactly
/// (<c>runtime_components.h:53-80</c>).
///
/// Unlike <see cref="CnaManagedGameCallbacks"/>, every handler here returns <c>void</c> -- there is
/// no <c>CNA_Result</c> and no <c>out_error</c>. A managed exception must still never unwind across
/// the <c>UnmanagedCallersOnly</c> boundary, so <c>CNA.GameComponent</c>'s wrappers catch
/// everything; with no error channel to report through, the exception is stashed and rethrown from
/// the next managed-initiated call instead of being swallowed. See that type's own doc comment.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaGameComponentCallbacks
{
    public uint StructSize;
    public uint StructVersion;
    public delegate* unmanaged[Cdecl]<nint, void> Initialize;
    public delegate* unmanaged[Cdecl]<CnaGameTime*, nint, void> Update;
    public delegate* unmanaged[Cdecl]<CnaGameTime*, nint, void> Draw;
    public delegate* unmanaged[Cdecl]<nint, void> LoadContent;
    public delegate* unmanaged[Cdecl]<nint, void> UnloadContent;
    public delegate* unmanaged[Cdecl]<nint, void> Dispose;
    public nint Context;

    public CnaGameComponentCallbacks()
    {
        StructSize = (uint)sizeof(CnaGameComponentCallbacks);
        StructVersion = 1;
    }
}
