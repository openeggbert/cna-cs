namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Byte4</c>: Four unsigned bytes. Unlike the normalized formats, the components are in [0, 255] rather than [0, 1].
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Byte4.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Byte4 : IPackedVector<uint>, IEquatable<Byte4>
{
    public Byte4(float x, float y, float z, float w)
    {
        PackedValue = Pack(x, y, z, w);
    }

    public Byte4(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(PackedValue & 0xFF, (PackedValue >> 8) & 0xFF, (PackedValue >> 16) & 0xFF, (PackedValue >> 24) & 0xFF);

    private static uint Pack(float x, float y, float z, float w)
    {
        uint xi = PackUtils.PackUnsigned(255f, x);
        uint yi = PackUtils.PackUnsigned(255f, y);
        uint zi = PackUtils.PackUnsigned(255f, z);
        uint wi = PackUtils.PackUnsigned(255f, w);
        return xi | (yi << 8) | (zi << 16) | (wi << 24);
    }

    public readonly bool Equals(Byte4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Byte4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Byte4 a, Byte4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Byte4 a, Byte4 b) => a.PackedValue != b.PackedValue;
}
