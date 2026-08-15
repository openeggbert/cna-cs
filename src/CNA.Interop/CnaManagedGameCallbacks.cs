using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Function-pointer callback table native CNA invokes to drive a managed <c>Game</c>. Native
/// CNA owns window creation, platform event pumping, timing, and the frame lifecycle; it calls
/// back into managed code at coarse per-frame boundaries. See the "Game inheritance requires a
/// callback bridge" design in ../../cnabinding/analysis_binding.md §20-§21.
///
/// Every function pointer must target a static method annotated with
/// <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/> -- see
/// CNA.Framework's ManagedGameBridge for the managed side of this contract.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaManagedGameCallbacks
{
    public delegate* unmanaged[Cdecl]<nint, void> Initialize;
    public delegate* unmanaged[Cdecl]<nint, void> LoadContent;
    public delegate* unmanaged[Cdecl]<nint, CnaGameTime, void> Update;
    public delegate* unmanaged[Cdecl]<nint, CnaGameTime, void> Draw;
    public delegate* unmanaged[Cdecl]<nint, void> UnloadContent;
}
