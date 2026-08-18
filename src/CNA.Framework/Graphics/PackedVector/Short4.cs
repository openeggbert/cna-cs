namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Short4</c>: Four signed shorts, unnormalized. See <see cref="Short2"/>.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Short4.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Short4 : IPackedVector<ulong>, IEquatable<Short4>
{
    public Short4(float x, float y, float z, float w)
    {
        PackedValue = Pack(x, y, z, w);
    }

    public Short4(Vector4 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
    }


    /// <summary>The raw storage word.</summary>
    public ulong PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);

    public readonly Vector4 ToVector4() => new Vector4(
            (short)(PackedValue & 0xFFFF),
            (short)((PackedValue >> 16) & 0xFFFF),
            (short)((PackedValue >> 32) & 0xFFFF),
            (short)((PackedValue >> 48) & 0xFFFF));

    private static ulong Pack(float x, float y, float z, float w)
    {
        ulong xi = (ushort)(short)Math.Clamp(x, -32768f, 32767f);
        ulong yi = (ushort)(short)Math.Clamp(y, -32768f, 32767f);
        ulong zi = (ushort)(short)Math.Clamp(z, -32768f, 32767f);
        ulong wi = (ushort)(short)Math.Clamp(w, -32768f, 32767f);
        return xi | (yi << 16) | (zi << 32) | (wi << 48);
    }

    public readonly bool Equals(Short4 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Short4 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Short4 a, Short4 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Short4 a, Short4 b) => a.PackedValue != b.PackedValue;
}
