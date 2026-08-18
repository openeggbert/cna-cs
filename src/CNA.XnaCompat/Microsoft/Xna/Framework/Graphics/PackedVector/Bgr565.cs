namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Bgr565</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Bgr565</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Bgr565 : IPackedVector<ushort>, IEquatable<Bgr565>
{
    public Bgr565(float r, float g, float b)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgr565(r, g, b).PackedValue;
    }

    public Bgr565(Vector3 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgr565(vector).PackedValue;
    }

    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    public readonly Vector3 ToVector3() => ToInner().ToVector3();

    private readonly CNA.Graphics.PackedVector.Bgr565 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Bgr565 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgr565 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Bgr565 a, Bgr565 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgr565 a, Bgr565 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Bgr565(Bgr565 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Bgr565(CNA.Graphics.PackedVector.Bgr565 value) =>
        new() { PackedValue = value.PackedValue };
}
