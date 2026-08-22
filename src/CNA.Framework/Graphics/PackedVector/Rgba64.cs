namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Rgba64</c>: Four unsigned shorts normalized to [0, 1].
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Rgba64.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Rgba64 : IPackedVector<ulong>, IEquatable<Rgba64>
{
    public Rgba64(float r, float g, float b, float a)
    {
        PackedValue = Pack(r, g, b, a);
    }

    public Rgba64(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ulong PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            PackUtils.UnpackUNorm(65535u, (uint)PackedValue),
            PackUtils.UnpackUNorm(65535u, (uint)(PackedValue >> 16)),
            PackUtils.UnpackUNorm(65535u, (uint)(PackedValue >> 32)),
            PackUtils.UnpackUNorm(65535u, (uint)(PackedValue >> 48)));

    private static ulong Pack(float r, float g, float b, float a)
    {
        ulong ri = PackUtils.PackUNorm(65535f, r);
        ulong gi = PackUtils.PackUNorm(65535f, g);
        ulong bi = PackUtils.PackUNorm(65535f, b);
        ulong ai = PackUtils.PackUNorm(65535f, a);
        return ri | (gi << 16) | (bi << 32) | (ai << 48);
    }

    public readonly bool Equals(Rgba64 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rgba64 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Rgba64 a, Rgba64 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rgba64 a, Rgba64 b) => a.PackedValue != b.PackedValue;
}
