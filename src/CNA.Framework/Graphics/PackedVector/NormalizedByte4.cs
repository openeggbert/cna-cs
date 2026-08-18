namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>NormalizedByte4</c>: Four signed bytes normalized to [-1, 1]. See <see cref="NormalizedByte2"/> for the rounding rule.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>NormalizedByte4.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct NormalizedByte4 : IPackedVector<uint>, IEquatable<NormalizedByte4>
{
    public NormalizedByte4(float x, float y, float z, float w)
    {
        PackedValue = Pack(x, y, z, w);
    }

    public NormalizedByte4(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            (sbyte)(PackedValue & 0xFF) / 127f,
            (sbyte)((PackedValue >> 8) & 0xFF) / 127f,
            (sbyte)((PackedValue >> 16) & 0xFF) / 127f,
            (sbyte)((PackedValue >> 24) & 0xFF) / 127f);

    private static uint Pack(float x, float y, float z, float w)
    {
        uint xi = (byte)(sbyte)MathF.Round(Math.Clamp(x, -1f, 1f) * 127f, MidpointRounding.AwayFromZero);
        uint yi = (byte)(sbyte)MathF.Round(Math.Clamp(y, -1f, 1f) * 127f, MidpointRounding.AwayFromZero);
        uint zi = (byte)(sbyte)MathF.Round(Math.Clamp(z, -1f, 1f) * 127f, MidpointRounding.AwayFromZero);
        uint wi = (byte)(sbyte)MathF.Round(Math.Clamp(w, -1f, 1f) * 127f, MidpointRounding.AwayFromZero);
        return xi | (yi << 8) | (zi << 16) | (wi << 24);
    }

    public readonly bool Equals(NormalizedByte4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedByte4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(NormalizedByte4 a, NormalizedByte4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedByte4 a, NormalizedByte4 b) => a.PackedValue != b.PackedValue;
}
