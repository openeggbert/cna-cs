namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Rg32</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Rg32</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Rg32 : IPackedVector<uint>, IEquatable<Rg32>
{
    public Rg32(float r, float g)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rg32(r, g).PackedValue;
    }

    public Rg32(Vector2 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rg32(vector).PackedValue;
    }

    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    public readonly Vector2 ToVector2() => ToInner().ToVector2();

    private readonly CNA.Graphics.PackedVector.Rg32 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Rg32 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rg32 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Rg32 a, Rg32 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rg32 a, Rg32 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Rg32(Rg32 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Rg32(CNA.Graphics.PackedVector.Rg32 value) =>
        new() { PackedValue = value.PackedValue };
}
