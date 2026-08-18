namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>HalfVector2</c>.
///
/// A duplicated value type with implicit conversions, the pattern this layer already uses for
/// <c>Vector3</c>/<c>Color</c>/<c>Point</c> -- a struct cannot inherit, and the interface it
/// implements is typed on this namespace's own <see cref="Vector4"/>. Every member delegates to
/// <c>CNA.Graphics.PackedVector.HalfVector2</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct HalfVector2 : IPackedVector<uint>, IEquatable<HalfVector2>
{
    public HalfVector2(float x, float y)
    {
        PackedValue = new CNA.Graphics.PackedVector.HalfVector2(x, y).PackedValue;
    }

    public HalfVector2(Vector2 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.HalfVector2(vector).PackedValue;
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

    private readonly CNA.Graphics.PackedVector.HalfVector2 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(HalfVector2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is HalfVector2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(HalfVector2 a, HalfVector2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(HalfVector2 a, HalfVector2 b) => a.PackedValue != b.PackedValue;

    public static implicit operator CNA.Graphics.PackedVector.HalfVector2(HalfVector2 value) =>
        new() { PackedValue = value.PackedValue };

    public static implicit operator HalfVector2(CNA.Graphics.PackedVector.HalfVector2 value) =>
        new() { PackedValue = value.PackedValue };
}
