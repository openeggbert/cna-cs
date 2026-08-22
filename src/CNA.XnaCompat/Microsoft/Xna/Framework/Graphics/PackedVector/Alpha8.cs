namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Alpha8</c>.
///
/// A separate value type is required because structs cannot inherit and the packed-vector
/// interface is typed on this namespace's own <see cref="Vector4"/>. Its public surface follows
/// XNA while the implementation delegates to
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

    void IPackedVector.PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector.ToFramework());
        PackedValue = inner.PackedValue;
    }

    readonly Vector4 IPackedVector.ToVector4() => Vector4.FromFramework(ToInner().ToVector4());

    public readonly float ToAlpha() => ToInner().ToAlpha();

    private readonly CNA.Graphics.PackedVector.Alpha8 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Alpha8 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Alpha8 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Alpha8 a, Alpha8 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Alpha8 a, Alpha8 b) => a.PackedValue != b.PackedValue;
}
