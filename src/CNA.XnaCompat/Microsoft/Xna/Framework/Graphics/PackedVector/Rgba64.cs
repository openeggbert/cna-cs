namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Rgba64</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Rgba64</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Rgba64 : IPackedVector<ulong>, IEquatable<Rgba64>
{
    public Rgba64(float r, float g, float b, float a)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rgba64(r, g, b, a).PackedValue;
    }

    public Rgba64(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Rgba64(vector).PackedValue;
    }

    public ulong PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    private readonly CNA.Graphics.PackedVector.Rgba64 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Rgba64 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rgba64 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Rgba64 a, Rgba64 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rgba64 a, Rgba64 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Rgba64(Rgba64 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Rgba64(CNA.Graphics.PackedVector.Rgba64 value) =>
        new() { PackedValue = value.PackedValue };
}
