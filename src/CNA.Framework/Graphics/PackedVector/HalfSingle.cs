namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>HalfSingle</c>: One IEEE 754 binary16 value. Conversion goes through <see cref="Half"/> rather than a hand-rolled bit shuffle -- design invariant #7, use the real BCL for a concept that is not CNA-specific.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>HalfSingle.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct HalfSingle : IPackedVector<ushort>, IEquatable<HalfSingle>
{
    public HalfSingle(float single)
    {
        PackedValue = Pack(single);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X);

    public readonly Vector4 ToVector4() => new Vector4(ToSingle(), 0f, 0f, 1f);

    /// <summary>Expands the half back to single precision.</summary>
    public readonly float ToSingle() => (float)BitConverter.UInt16BitsToHalf(PackedValue);

    private static ushort Pack(float single)
        => BitConverter.HalfToUInt16Bits((Half)single);

    public readonly bool Equals(HalfSingle other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is HalfSingle other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(HalfSingle a, HalfSingle b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(HalfSingle a, HalfSingle b) => a.PackedValue != b.PackedValue;
}
