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
        : this(handleValue, release, ownsHandle: true)
    {
    }

    /// <summary>
    /// <paramref name="ownsHandle"/> <see langword="false"/> wraps a handle this object must
    /// <em>not</em> release -- the C API hands several out as explicitly borrowed, with a lifetime
    /// the real owner controls (<c>cna_video_player_get_texture</c>'s frame texture is the case
    /// this was added for: "valid only until the next call on this player").
    ///
    /// Without it, a borrowed handle wrapped here would be destroyed by
    /// <see cref="SafeHandle"/>'s critical finalizer whether or not anyone called
    /// <c>Dispose</c> -- a use-after-free the owner could not prevent, and one a doc comment
    /// telling callers "do not dispose this" cannot stop either.
    /// </summary>
    public NativeResourceHandle(nint handleValue, Action<nint> release, bool ownsHandle)
        : base(IntPtr.Zero, ownsHandle)
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
