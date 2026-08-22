namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Rg32</c>: Two unsigned shorts normalized to [0, 1].
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Rg32.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Rg32 : IPackedVector<uint>, IEquatable<Rg32>
{
    public Rg32(float r, float g)
    {
        PackedValue = Pack(r, g);
    }

    public Rg32(Vector2 vector)
    {
        PackedValue = Pack(vector.X, vector.Y);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y);

    public readonly Vector4 ToVector4() => new Vector4(PackUtils.UnpackUNorm(65535u, PackedValue), PackUtils.UnpackUNorm(65535u, PackedValue >> 16), 0f, 1f);

    public readonly Vector2 ToVector2() => new(PackUtils.UnpackUNorm(65535u, PackedValue), PackUtils.UnpackUNorm(65535u, PackedValue >> 16));

    private static uint Pack(float r, float g)
    {
        uint ri = PackUtils.PackUNorm(65535f, r);
        uint gi = PackUtils.PackUNorm(65535f, g);
        return ri | (gi << 16);
    }

    public readonly bool Equals(Rg32 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rg32 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Rg32 a, Rg32 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rg32 a, Rg32 b) => a.PackedValue != b.PackedValue;
}
