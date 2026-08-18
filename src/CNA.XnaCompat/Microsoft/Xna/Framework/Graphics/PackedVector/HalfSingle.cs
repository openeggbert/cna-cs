namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>HalfSingle</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.HalfSingle</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct HalfSingle : IPackedVector<ushort>, IEquatable<HalfSingle>
{
    public HalfSingle(float single)
    {
        PackedValue = new CNA.Graphics.PackedVector.HalfSingle(single).PackedValue;
    }

    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    public readonly float ToSingle() => ToInner().ToSingle();

    private readonly CNA.Graphics.PackedVector.HalfSingle ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(HalfSingle other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is HalfSingle other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(HalfSingle a, HalfSingle b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(HalfSingle a, HalfSingle b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.HalfSingle(HalfSingle value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator HalfSingle(CNA.Graphics.PackedVector.HalfSingle value) =>
        new() { PackedValue = value.PackedValue };
}
