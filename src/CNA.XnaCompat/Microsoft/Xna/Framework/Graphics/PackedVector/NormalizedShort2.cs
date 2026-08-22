namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>NormalizedShort2</c>.
///
/// A separate value type is required because structs cannot inherit and the packed-vector
/// interface is typed on this namespace's own <see cref="Vector4"/>. Its public surface follows
/// XNA while the implementation delegates to
/// <c>CNA.Graphics.PackedVector.NormalizedShort2</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct NormalizedShort2 : IPackedVector<uint>, IEquatable<NormalizedShort2>
{
    public NormalizedShort2(float x, float y)
    {
        PackedValue = new CNA.Graphics.PackedVector.NormalizedShort2(x, y).PackedValue;
    }

    public NormalizedShort2(Vector2 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.NormalizedShort2(vector.ToFramework()).PackedValue;
    }

#pragma warning disable CS3021 // XNA carries this member attribute even without an assembly-level CLS declaration.
    [CLSCompliant(false)]
    public uint PackedValue { get; set; }
#pragma warning restore CS3021

    void IPackedVector.PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector.ToFramework());
        PackedValue = inner.PackedValue;
    }

    readonly Vector4 IPackedVector.ToVector4() => Vector4.FromFramework(ToInner().ToVector4());

    public readonly Vector2 ToVector2() => Vector2.FromFramework(ToInner().ToVector2());

    private readonly CNA.Graphics.PackedVector.NormalizedShort2 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(NormalizedShort2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedShort2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(NormalizedShort2 a, NormalizedShort2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedShort2 a, NormalizedShort2 b) => a.PackedValue != b.PackedValue;
}
