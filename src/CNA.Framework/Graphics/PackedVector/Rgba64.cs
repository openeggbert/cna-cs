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
            (PackedValue & 0xFFFF) / 65535f,
            ((PackedValue >> 16) & 0xFFFF) / 65535f,
            ((PackedValue >> 32) & 0xFFFF) / 65535f,
            ((PackedValue >> 48) & 0xFFFF) / 65535f);

    private static ulong Pack(float r, float g, float b, float a)
    {
        ulong ri = (ulong)(Math.Clamp(r, 0f, 1f) * 65535f + 0.5f);
        ulong gi = (ulong)(Math.Clamp(g, 0f, 1f) * 65535f + 0.5f);
        ulong bi = (ulong)(Math.Clamp(b, 0f, 1f) * 65535f + 0.5f);
        ulong ai = (ulong)(Math.Clamp(a, 0f, 1f) * 65535f + 0.5f);
        return ri | (gi << 16) | (bi << 32) | (ai << 48);
    }

    public readonly bool Equals(Rgba64 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Rgba64 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Rgba64 a, Rgba64 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Rgba64 a, Rgba64 b) => a.PackedValue != b.PackedValue;
}
