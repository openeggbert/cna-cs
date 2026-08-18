namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>HalfVector4</c>: Four IEEE 754 binary16 values. See <see cref="HalfSingle"/> for why the conversion uses <see cref="Half"/>.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>HalfVector4.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct HalfVector4 : IPackedVector<ulong>, IEquatable<HalfVector4>
{
    public HalfVector4(float x, float y, float z, float w)
    {
        PackedValue = Pack(x, y, z, w);
    }

    public HalfVector4(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ulong PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            (float)BitConverter.UInt16BitsToHalf((ushort)(PackedValue & 0xFFFF)),
            (float)BitConverter.UInt16BitsToHalf((ushort)((PackedValue >> 16) & 0xFFFF)),
            (float)BitConverter.UInt16BitsToHalf((ushort)((PackedValue >> 32) & 0xFFFF)),
            (float)BitConverter.UInt16BitsToHalf((ushort)((PackedValue >> 48) & 0xFFFF)));

    private static ulong Pack(float x, float y, float z, float w)
    {
        return BitConverter.HalfToUInt16Bits((Half)x)
            | ((ulong)BitConverter.HalfToUInt16Bits((Half)y) << 16)
            | ((ulong)BitConverter.HalfToUInt16Bits((Half)z) << 32)
            | ((ulong)BitConverter.HalfToUInt16Bits((Half)w) << 48);
    }

    public readonly bool Equals(HalfVector4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is HalfVector4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(HalfVector4 a, HalfVector4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(HalfVector4 a, HalfVector4 b) => a.PackedValue != b.PackedValue;
}
