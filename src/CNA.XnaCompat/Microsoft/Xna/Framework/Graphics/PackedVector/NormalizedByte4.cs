namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>NormalizedByte4</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.NormalizedByte4</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct NormalizedByte4 : IPackedVector<uint>, IEquatable<NormalizedByte4>
{
    public NormalizedByte4(float x, float y, float z, float w)
    {
        PackedValue = new CNA.Graphics.PackedVector.NormalizedByte4(x, y, z, w).PackedValue;
    }

    public NormalizedByte4(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.NormalizedByte4(vector).PackedValue;
    }

    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    private readonly CNA.Graphics.PackedVector.NormalizedByte4 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(NormalizedByte4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedByte4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(NormalizedByte4 a, NormalizedByte4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedByte4 a, NormalizedByte4 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.NormalizedByte4(NormalizedByte4 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator NormalizedByte4(CNA.Graphics.PackedVector.NormalizedByte4 value) =>
        new() { PackedValue = value.PackedValue };
}
