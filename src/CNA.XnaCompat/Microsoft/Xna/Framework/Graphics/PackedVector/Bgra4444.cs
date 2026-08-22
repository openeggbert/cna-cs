namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Bgra4444</c>.
///
/// A separate value type is required because structs cannot inherit and the packed-vector
/// interface is typed on this namespace's own <see cref="Vector4"/>. Its public surface follows
/// XNA while the implementation delegates to
/// <c>CNA.Graphics.PackedVector.Bgra4444</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Bgra4444 : IPackedVector<ushort>, IEquatable<Bgra4444>
{
    public Bgra4444(float x, float y, float z, float w)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgra4444(x, y, z, w).PackedValue;
    }

    public Bgra4444(Vector4 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgra4444(vector.ToFramework()).PackedValue;
    }

#pragma warning disable CS3021 // XNA carries this member attribute even without an assembly-level CLS declaration.
    [CLSCompliant(false)]
    public ushort PackedValue { get; set; }
#pragma warning restore CS3021

    void IPackedVector.PackFromVector4(Vector4 vector)
    {
        var inner = ToInner();
        inner.PackFromVector4(vector.ToFramework());
        PackedValue = inner.PackedValue;
    }

    public readonly Vector4 ToVector4() => Vector4.FromFramework(ToInner().ToVector4());

    private readonly CNA.Graphics.PackedVector.Bgra4444 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Bgra4444 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgra4444 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Bgra4444 a, Bgra4444 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgra4444 a, Bgra4444 b) => a.PackedValue != b.PackedValue;
}
