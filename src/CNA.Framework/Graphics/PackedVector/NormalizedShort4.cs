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
            (short)(PackedValue & 0xFFFF) / 32767f,
            (short)((PackedValue >> 16) & 0xFFFF) / 32767f,
            (short)((PackedValue >> 32) & 0xFFFF) / 32767f,
            (short)((PackedValue >> 48) & 0xFFFF) / 32767f);

    private static ulong Pack(float x, float y, float z, float w)
    {
        ulong xi = (ushort)(short)MathF.Round(Math.Clamp(x, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        ulong yi = (ushort)(short)MathF.Round(Math.Clamp(y, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        ulong zi = (ushort)(short)MathF.Round(Math.Clamp(z, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        ulong wi = (ushort)(short)MathF.Round(Math.Clamp(w, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        return xi | (yi << 16) | (zi << 32) | (wi << 48);
    }

    public readonly bool Equals(NormalizedShort4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedShort4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(NormalizedShort4 a, NormalizedShort4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedShort4 a, NormalizedShort4 b) => a.PackedValue != b.PackedValue;
}
