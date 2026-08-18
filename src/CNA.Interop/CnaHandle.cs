using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// An opaque, generation-checked native resource handle -- matches the real, shipped
/// openeggbert/cna C API's own <c>CNA_Handle</c> exactly (<c>typedef uint64_t CNA_Handle;</c>,
/// <c>abi.h:104</c>): always a fixed-width 64-bit value, not pointer-width. A prior version of
/// this type used <see cref="nint"/> as its backing field, on the (correct, for every platform
/// this project targets) assumption that pointer-width and 64-bit coincide -- the backing field
/// is now the real wire type exactly, rather than relying on that coincidence, now that a real
/// ABI exists to confirm against (see <c>NEXT.md</c>'s native-ABI-migration entry). The
/// <see cref="nint"/> constructor overload is kept so every existing call site building a
/// <see cref="CnaHandle"/> from a <see cref="NativeResourceHandle"/>'s own <see cref="nint"/>
/// storage (<c>DangerousGetHandle()</c>) keeps compiling unchanged -- converting at this one
/// boundary point, rather than at every call site individually.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaHandle : IEquatable<CnaHandle>
{
    public readonly ulong Value;

    public CnaHandle(ulong value) => Value = value;

    /// <summary>Converts from the pointer-width handle storage <see cref="NativeResourceHandle"/>
    /// (and every native-backed managed wrapper's own <c>NativeHandleValue</c>) still uses --
    /// always exact and lossless on every platform this project targets (CNA is 64-bit-only), the
    /// same reasoning this type's own constructor used to rely on for its whole representation.</summary>
    public CnaHandle(nint value) => Value = unchecked((ulong)value);

    /// <summary>The inverse of the <see cref="nint"/> constructor above -- narrows back to
    /// pointer-width storage for the many call sites across this codebase that still store a
    /// resource's native identity as <see cref="nint"/> (<c>NativeResourceHandle</c>'s own
    /// storage, every native-backed managed wrapper's own <c>NativeHandleValue</c>). Checked, not
    /// unchecked: this narrowing is only exact on the 64-bit-only platforms this project targets,
    /// unlike the widening conversion the <see cref="nint"/> constructor performs, which is exact
    /// on any width -- a violated assumption here should throw, not silently truncate a
    /// handle.</summary>
    public nint AsNint => checked((nint)Value);

    /// <summary>Matches the real ABI's own <c>CNA_INVALID_HANDLE</c> (<c>abi.h:107</c>, value 0).</summary>
    public static readonly CnaHandle Zero = new(0UL);

    public bool IsNull => Value == 0;

    public bool Equals(CnaHandle other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is CnaHandle other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"0x{Value:x}";

    public static bool operator ==(CnaHandle left, CnaHandle right) => left.Equals(right);
    public static bool operator !=(CnaHandle left, CnaHandle right) => !left.Equals(right);
}
