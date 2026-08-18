namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Rgba1010102</c>: Normalized RGBA 10:10:10:2. Alpha has two bits, so it quantizes to four levels.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Rgba1010102.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Rgba1010102 : IPackedVector<uint>, IEquatable<Rgba1010102>
{
    public Rgba1010102(float r, float g, float b, float a)
    {
        PackedValue = Pack(r, g, b, a);
    }

    public Rgba1010102(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            (PackedValue & 0x3FF) / 1023f,
            ((PackedValue >> 10) & 0x3FF) / 1023f,
            ((PackedValue >> 20) & 0x3FF) / 1023f,
            ((PackedValue >> 30) & 0x003) / 3f);

    private static uint Pack(float r, float g, float b, float a)
    {
        uint ri = (uint)(Math.Clamp(r, 0f, 1f) * 1023f + 0.5f);
        uint gi = (uint)(Math.Clamp(g, 0f, 1f) * 1023f + 0.5f);
        uint bi = (uint)(Math.Clamp(b, 0f, 1f) * 1023f + 0.5f);
        uint ai = (uint)(Math.Clamp(a, 0f, 1f) * 3f + 0.5f);
        return ri | (gi << 10) | (bi << 20) | (ai << 30);
    }

    public readonly bool Equals(Rgba1010102 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rgba1010102 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Rgba1010102 a, Rgba1010102 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rgba1010102 a, Rgba1010102 b) => a.PackedValue != b.PackedValue;
}
