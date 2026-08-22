namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Bgr565</c>: 16-bit normalized BGR 5:6:5. Green gets the extra bit, which is why its divisor is 63 and the others' is 31.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Bgr565.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Bgr565 : IPackedVector<ushort>, IEquatable<Bgr565>
{
    public Bgr565(float r, float g, float b)
    {
        PackedValue = Pack(r, g, b);
    }

    public Bgr565(Vector3 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z);

    public readonly Vector4 ToVector4() => new Vector4(PackUtils.UnpackUNorm(31u, (uint)PackedValue >> 11), PackUtils.UnpackUNorm(63u, (uint)PackedValue >> 5), PackUtils.UnpackUNorm(31u, PackedValue), 1f);

    /// <summary>The three channels expanded back to [0, 1].</summary>
    public readonly Vector3 ToVector3() => new(PackUtils.UnpackUNorm(31u, (uint)PackedValue >> 11), PackUtils.UnpackUNorm(63u, (uint)PackedValue >> 5), PackUtils.UnpackUNorm(31u, PackedValue));

    private static ushort Pack(float r, float g, float b)
    {
        uint ri = PackUtils.PackUNorm(31f, r);
        uint gi = PackUtils.PackUNorm(63f, g);
        uint bi = PackUtils.PackUNorm(31f, b);
        return (ushort)((ri << 11) | (gi << 5) | bi);
    }

    public readonly bool Equals(Bgr565 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgr565 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Bgr565 a, Bgr565 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgr565 a, Bgr565 b) => a.PackedValue != b.PackedValue;
}
