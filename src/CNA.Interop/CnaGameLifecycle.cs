using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_CallbackError</c> exactly
/// (<c>runtime.h:32-41</c>) -- a CNA-initialized (not caller-initialized) diagnostic structure
/// passed to every lifecycle callback so it can report why it failed. Real XNA/MonoGame Update or
/// Draw overrides can and do throw; a managed exception must never unwind across the
/// <c>UnmanagedCallersOnly</c> boundary (undefined behavior), so <see cref="CNA.Game"/>'s callback
/// wrappers catch it, encode its message into a pinned buffer they own, and populate
/// <see cref="Message"/> to point at it before returning <c>CNA_RESULT_CALLBACK</c> -- see
/// <c>Game.cs</c>'s own callback-wrapper doc comments for why that buffer must be pinned for the
/// <see cref="CNA.Game"/> instance's whole lifetime rather than `fixed`-pinned only for the
/// duration of the method that builds it.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaCallbackError
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly CnaStringView Message;
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GameFrameHooks</c> exactly
/// (<c>runtime.h:119-149</c>) -- a **second**, optional callback table installed after
/// <c>cna_game_create</c> via <c>cna_game_set_frame_hooks_ext</c>, deliberately kept separate from
/// <see cref="CnaManagedGameCallbacks"/> upstream so appending to it never breaks an existing
/// consumer's positional initializer. <c>CNA.Game</c> only wires <see cref="Initialize"/> today
/// (the one hook it already had a virtual method for before this migration); <see cref="BeginRun"/>/
/// <see cref="EndRun"/>/<see cref="BeginDraw"/>/<see cref="EndDraw"/> are left null (a null member is
/// simply never called, matching the component-callback table's own rule) since <c>CNA.Game</c> has
/// no matching virtual methods to invoke yet -- real feature surface for a future session, not
/// added speculatively here.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaGameFrameHooks
{
    public uint StructSize;
    public uint StructVersion;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> Initialize;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> BeginRun;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> EndRun;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, byte*, CnaCallbackError*, CnaResult> BeginDraw;
    public delegate* unmanaged[Cdecl]<CnaHandle, CnaGameTime*, nint, CnaCallbackError*, CnaResult> EndDraw;
    public nint Context;
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GameCreateInfo</c> exactly
/// (<c>runtime.h:169-190</c>). Per <c>docs/c-api/ABI_VERSIONING.md</c>'s "POD structs" section, the
/// caller must set <see cref="StructSize"/> to <c>sizeof(CnaGameCreateInfo)</c> and
/// <see cref="StructVersion"/> to <c>1</c> before calling <see cref="Native.cna_game_create"/> --
/// see <c>CnaError.cs</c>'s doc comment for the same convention, confirmed there against
/// <c>CnaCApi.cpp</c>'s own validation. <see cref="Reserved"/> exists only so this struct's layout
/// matches the real one byte-for-byte (needed for <see cref="TargetElapsedTimeTicks"/> to land at
/// the real struct's own offset); it carries no meaning of its own and the real header requires
/// callers to zero it, which the default `struct` value already does.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaGameCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public byte IsFixedTimeStep;
    public CnaReservedBytes7 Reserved;
    public long TargetElapsedTimeTicks;
    public CnaStringView WindowTitle;
    public CnaManagedGameCallbacks* Callbacks;
}

/// <summary>Seven reserved padding bytes, matching <c>CNA_GameCreateInfo::reserved</c>
/// (<c>runtime.h:180</c>) byte-for-byte -- see <see cref="CnaGlyphBuffer"/> for why this project
/// uses the C# 12 <c>InlineArray</c> feature for fixed-size inline buffers like this one.</summary>
[InlineArray(7)]
internal struct CnaReservedBytes7
{
    private byte _element0;
}
