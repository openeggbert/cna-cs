namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Bgra5551</c>: 16-bit normalized BGRA 5:5:5:1. Alpha is one bit, so it rounds to fully opaque or fully transparent and nothing between.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Bgra5551.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Bgra5551 : IPackedVector<ushort>, IEquatable<Bgra5551>
{
    public Bgra5551(float r, float g, float b, float a)
    {
        PackedValue = Pack(r, g, b, a);
    }

    public Bgra5551(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(PackUtils.UnpackUNorm(31u, (uint)PackedValue >> 10), PackUtils.UnpackUNorm(31u, (uint)PackedValue >> 5), PackUtils.UnpackUNorm(31u, PackedValue), PackUtils.UnpackUNorm(1u, (uint)PackedValue >> 15));

    private static ushort Pack(float r, float g, float b, float a)
    {
        uint ri = PackUtils.PackUNorm(31f, r);
        uint gi = PackUtils.PackUNorm(31f, g);
        uint bi = PackUtils.PackUNorm(31f, b);
        uint ai = PackUtils.PackUNorm(1f, a);
        return (ushort)((ai << 15) | (ri << 10) | (gi << 5) | bi);
    }

    public readonly bool Equals(Bgra5551 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgra5551 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Bgra5551 a, Bgra5551 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgra5551 a, Bgra5551 b) => a.PackedValue != b.PackedValue;
}
