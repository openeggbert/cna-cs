namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Rgba1010102</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Rgba1010102</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Rgba1010102 : IPackedVector<uint>, IEquatable<Rgba1010102>
{
    public Rgba1010102(float r, float g, float b, float a)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rgba1010102(r, g, b, a).PackedValue;
    }

    public Rgba1010102(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rgba1010102(vector).PackedValue;
    }

    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    private readonly CNA.Graphics.PackedVector.Rgba1010102 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Rgba1010102 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rgba1010102 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Rgba1010102 a, Rgba1010102 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rgba1010102 a, Rgba1010102 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Rgba1010102(Rgba1010102 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Rgba1010102(CNA.Graphics.PackedVector.Rgba1010102 value) =>
        new() { PackedValue = value.PackedValue };
}
