namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>NormalizedShort2</c>: Two signed shorts normalized to [-1, 1]. See <see cref="NormalizedByte2"/> for the rounding rule.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>NormalizedShort2.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct NormalizedShort2 : IPackedVector<uint>, IEquatable<NormalizedShort2>
{
    public NormalizedShort2(float x, float y)
    {
        PackedValue = Pack(x, y);
    }

    public NormalizedShort2(Vector2 vector)
    {
        PackedValue = Pack(vector.X, vector.Y);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y);

    public readonly Vector4 ToVector4() => new Vector4((short)(PackedValue & 0xFFFF) / 32767f, (short)((PackedValue >> 16) & 0xFFFF) / 32767f, 0f, 1f);

    public readonly Vector2 ToVector2() => new((short)(PackedValue & 0xFFFF) / 32767f, (short)((PackedValue >> 16) & 0xFFFF) / 32767f);

    private static uint Pack(float x, float y)
    {
        uint xi = (ushort)(short)MathF.Round(Math.Clamp(x, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        uint yi = (ushort)(short)MathF.Round(Math.Clamp(y, -1f, 1f) * 32767f, MidpointRounding.AwayFromZero);
        return xi | (yi << 16);
    }

    public readonly bool Equals(NormalizedShort2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is NormalizedShort2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(NormalizedShort2 a, NormalizedShort2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(NormalizedShort2 a, NormalizedShort2 b) => a.PackedValue != b.PackedValue;
}
