namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>NormalizedShort4</c>: Four signed shorts normalized to [-1, 1]. See <see cref="NormalizedByte2"/> for the rounding rule.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>NormalizedShort4.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct NormalizedShort4 : IPackedVector<ulong>, IEquatable<NormalizedShort4>
{
    public NormalizedShort4(float x, float y, float z, float w)
    {
        PackedValue = Pack(x, y, z, w);
    }

    public NormalizedShort4(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ulong PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            PackUtils.UnpackSNorm(65535u, (uint)PackedValue),
            PackUtils.UnpackSNorm(65535u, (uint)(PackedValue >> 16)),
            PackUtils.UnpackSNorm(65535u, (uint)(PackedValue >> 32)),
            PackUtils.UnpackSNorm(65535u, (uint)(PackedValue >> 48)));

    private static ulong Pack(float x, float y, float z, float w)
    {
        ulong xi = PackUtils.PackSNorm(65535u, x);
        ulong yi = PackUtils.PackSNorm(65535u, y);
        ulong zi = PackUtils.PackSNorm(65535u, z);
        ulong wi = PackUtils.PackSNorm(65535u, w);
        return xi | (yi << 16) | (zi << 32) | (wi << 48);
    }

    public readonly bool Equals(NormalizedShort4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedShort4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(NormalizedShort4 a, NormalizedShort4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedShort4 a, NormalizedShort4 b) => a.PackedValue != b.PackedValue;
}
