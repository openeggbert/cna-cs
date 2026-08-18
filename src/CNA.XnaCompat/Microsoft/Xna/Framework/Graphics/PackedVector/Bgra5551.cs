namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Bgra5551</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Bgra5551</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Bgra5551 : IPackedVector<ushort>, IEquatable<Bgra5551>
{
    public Bgra5551(float r, float g, float b, float a)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgra5551(r, g, b, a).PackedValue;
    }

    public Bgra5551(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgra5551(vector).PackedValue;
    }

    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    private readonly CNA.Graphics.PackedVector.Bgra5551 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Bgra5551 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgra5551 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Bgra5551 a, Bgra5551 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgra5551 a, Bgra5551 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Bgra5551(Bgra5551 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Bgra5551(CNA.Graphics.PackedVector.Bgra5551 value) =>
        new() { PackedValue = value.PackedValue };
}
