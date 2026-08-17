using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GameCallbacks</c> exactly
/// (<c>runtime.h:82-106</c>). A prior version of this type had a different shape entirely, guessed
/// before any real ABI existed: five <c>void</c>-returning callbacks including <c>Initialize</c>
/// (now a separate, optional hook -- see <see cref="CnaGameFrameHooks"/>) and no
/// <see cref="Exiting"/>, each taking only a raw <c>nint context</c> (and, for
/// <see cref="Update"/>/<see cref="Draw"/>, a <see cref="CnaGameTime"/> *by value*). Every real
/// callback here instead returns <see cref="CnaResult"/> (a failure stops the game, reported as
/// <c>CNA_RESULT_CALLBACK</c>), takes <c>game_time</c> as a nullable *pointer* (null for
/// <see cref="LoadContent"/>/<see cref="UnloadContent"/>/<see cref="Exiting"/>), and takes an
/// additional <c>out_error</c> parameter the callback populates on failure -- see
/// <see cref="CnaCallbackError"/> and <c>CNA.Game</c>'s own callback-wrapper doc comments for how
/// that diagnostic crosses back out safely. <see cref="Exiting"/> has no matching <c>CNA.Game</c>
/// virtual method yet (real feature surface for a future session) and is left null when this
/// project builds its own instance of this struct -- "a null member is simply not called".
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaManagedGameCallbacks
{
    public uint StructSize;
    public uint StructVersion;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> LoadContent;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> Update;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> Draw;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> UnloadContent;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> Exiting;
    public nint Context;

    /// <summary>See <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for why this
    /// self-populates rather than relying on every call site to remember to.</summary>
    public CnaManagedGameCallbacks()
    {
        StructSize = (uint)sizeof(CnaManagedGameCallbacks);
        StructVersion = 1;
    }
}
