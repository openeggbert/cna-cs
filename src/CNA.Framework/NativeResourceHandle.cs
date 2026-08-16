using System.Runtime.InteropServices;

namespace CNA;

/// <summary>
/// A general-purpose <see cref="SafeHandle"/> for CNA native resources, parameterized by the
/// release callback for the specific resource type. Every native-backed CNA type
/// (<c>Texture2D</c>, <c>SpriteBatch</c>, ...) owns one of these rather than a bare handle value,
/// so normal disposal, forgotten disposal, and GC finalization are all handled uniformly. See
/// ../../cnabinding/analysis_binding.md §24 and plan.md invariant #4.
/// </summary>
internal sealed class NativeResourceHandle : SafeHandle
{
    private readonly Action<nint> _release;

    public NativeResourceHandle(nint handleValue, Action<nint> release)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        _release = release;
        SetHandle(handleValue);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        _release(handle);
        return true;
    }
}
