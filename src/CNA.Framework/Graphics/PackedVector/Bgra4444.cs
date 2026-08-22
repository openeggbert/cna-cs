namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Bgra4444</c>: 16-bit normalized BGRA 4:4:4:4.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Bgra4444.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Bgra4444 : IPackedVector<ushort>, IEquatable<Bgra4444>
{
    public Bgra4444(float r, float g, float b, float a)
    {
        PackedValue = Pack(r, g, b, a);
    }

    public Bgra4444(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(PackUtils.UnpackUNorm(15u, (uint)PackedValue >> 8), PackUtils.UnpackUNorm(15u, (uint)PackedValue >> 4), PackUtils.UnpackUNorm(15u, PackedValue), PackUtils.UnpackUNorm(15u, (uint)PackedValue >> 12));

    private static ushort Pack(float r, float g, float b, float a)
    {
        uint ri = PackUtils.PackUNorm(15f, r);
        uint gi = PackUtils.PackUNorm(15f, g);
        uint bi = PackUtils.PackUNorm(15f, b);
        uint ai = PackUtils.PackUNorm(15f, a);
        return (ushort)((ai << 12) | (ri << 8) | (gi << 4) | bi);
    }

    public readonly bool Equals(Bgra4444 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgra4444 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Bgra4444 a, Bgra4444 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgra4444 a, Bgra4444 b) => a.PackedValue != b.PackedValue;
}
