namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Alpha8</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.Alpha8</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Alpha8 : IPackedVector<byte>, IEquatable<Alpha8>
{
    public Alpha8(float alpha)
    {
        PackedValue = new CNA.Graphics.PackedVector.Alpha8(alpha).PackedValue;
    }

    public byte PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector);
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => ToInner().ToVector4();

    public readonly float ToAlpha() => ToInner().ToAlpha();

    private readonly CNA.Graphics.PackedVector.Alpha8 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Alpha8 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Alpha8 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Alpha8 a, Alpha8 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Alpha8 a, Alpha8 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.Alpha8(Alpha8 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator Alpha8(CNA.Graphics.PackedVector.Alpha8 value) =>
        new() { PackedValue = value.PackedValue };
}
