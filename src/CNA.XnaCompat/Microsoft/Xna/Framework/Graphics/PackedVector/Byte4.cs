namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Byte4</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Byte4</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Byte4 : IPackedVector<uint>, IEquatable<Byte4>
{
    public Byte4(float x, float y, float z, float w)
    {
        PackedValue = new CNA.Graphics.PackedVector.Byte4(x, y, z, w).PackedValue;
    }

    public Byte4(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Byte4(vector).PackedValue;
    }

    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    private readonly CNA.Graphics.PackedVector.Byte4 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Byte4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Byte4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Byte4 a, Byte4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Byte4 a, Byte4 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Byte4(Byte4 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Byte4(CNA.Graphics.PackedVector.Byte4 value) =>
        new() { PackedValue = value.PackedValue };
}
