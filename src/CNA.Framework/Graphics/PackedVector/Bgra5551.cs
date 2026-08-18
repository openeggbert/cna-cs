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

    public readonly Vector4 ToVector4() => new Vector4(((PackedValue >> 10) & 0x1F) / 31f, ((PackedValue >> 5) & 0x1F) / 31f, (PackedValue & 0x1F) / 31f, ((PackedValue >> 15) & 0x01) != 0 ? 1f : 0f);

    private static ushort Pack(float r, float g, float b, float a)
    {
        uint ri = (uint)(Math.Clamp(r, 0f, 1f) * 31f + 0.5f);
        uint gi = (uint)(Math.Clamp(g, 0f, 1f) * 31f + 0.5f);
        uint bi = (uint)(Math.Clamp(b, 0f, 1f) * 31f + 0.5f);
        uint ai = (uint)(Math.Clamp(a, 0f, 1f) + 0.5f);
        return (ushort)((ai << 15) | (ri << 10) | (gi << 5) | bi);
    }

    public readonly bool Equals(Bgra5551 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgra5551 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Bgra5551 a, Bgra5551 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgra5551 a, Bgra5551 b) => a.PackedValue != b.PackedValue;
}
