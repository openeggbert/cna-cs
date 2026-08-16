using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// An opaque, generation-checked native resource handle. See
/// ../../cnabinding/analysis_binding.md §9. Sized as <see cref="nint"/> so it interoperates
/// directly with <see cref="System.Runtime.InteropServices.SafeHandle"/> on the CNA
/// side, rather than as the wire-format <c>uint64_t</c> shown in the design docs -- CNA only
/// targets pointer-width (32/64-bit) platforms, so this is a lossless simplification.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaHandle : IEquatable<CnaHandle>
{
    public readonly nint Value;

    public CnaHandle(nint value) => Value = value;

    public static readonly CnaHandle Zero = new(0);

    public bool IsNull => Value == 0;

    public bool Equals(CnaHandle other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is CnaHandle other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"0x{Value:x}";

    public static bool operator ==(CnaHandle left, CnaHandle right) => left.Equals(right);
    public static bool operator !=(CnaHandle left, CnaHandle right) => !left.Equals(right);
}
