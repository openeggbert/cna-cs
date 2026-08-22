namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>NormalizedByte2</c>: Two signed bytes normalized to [-1, 1].
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>NormalizedByte2.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct NormalizedByte2 : IPackedVector<ushort>, IEquatable<NormalizedByte2>
{
    public NormalizedByte2(float x, float y)
    {
        PackedValue = Pack(x, y);
    }

    public NormalizedByte2(Vector2 vector)
    {
        PackedValue = Pack(vector.X, vector.Y);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y);

    public readonly Vector4 ToVector4() => new Vector4(PackUtils.UnpackSNorm(255u, PackedValue), PackUtils.UnpackSNorm(255u, (uint)PackedValue >> 8), 0f, 1f);

    public readonly Vector2 ToVector2() => new(PackUtils.UnpackSNorm(255u, PackedValue), PackUtils.UnpackSNorm(255u, (uint)PackedValue >> 8));

    private static ushort Pack(float x, float y)
    {
        uint xi = PackUtils.PackSNorm(255u, x);
        uint yi = PackUtils.PackSNorm(255u, y);
        return (ushort)(xi | (yi << 8));
    }

    public readonly bool Equals(NormalizedByte2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedByte2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(NormalizedByte2 a, NormalizedByte2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedByte2 a, NormalizedByte2 b) => a.PackedValue != b.PackedValue;
}
