namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>
/// XNA 4.0-compatible <c>Bgr565</c>.
///
/// A separate value type is required because structs cannot inherit and the packed-vector
/// interface is typed on this namespace's own <see cref="Vector4"/>. Its public surface follows
/// XNA while the implementation delegates to
/// <c>CNA.Graphics.PackedVector.Bgr565</c> rather than repeating the packing arithmetic, so there
/// is exactly one definition of the format.
/// </summary>
public struct Bgr565 : IPackedVector<ushort>, IEquatable<Bgr565>
{
    public Bgr565(float x, float y, float z)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgr565(x, y, z).PackedValue;
    }

    public Bgr565(Vector3 vector)
    {
        PackedValue = new CNA.Graphics.PackedVector.Bgr565(vector.ToFramework()).PackedValue;
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

    readonly Vector4 IPackedVector.ToVector4() => Vector4.FromFramework(ToInner().ToVector4());

    public readonly Vector3 ToVector3() => Vector3.FromFramework(ToInner().ToVector3());

    private readonly CNA.Graphics.PackedVector.Bgr565 ToInner() =>
        new() { PackedValue = PackedValue };

    public readonly bool Equals(Bgr565 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgr565 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Bgr565 a, Bgr565 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgr565 a, Bgr565 b) => a.PackedValue != b.PackedValue;
}
