namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Short2</c>: Two signed shorts, unnormalized -- the components are whole numbers in [-32768, 32767], not [-1, 1].
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Short2.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Short2 : IPackedVector<uint>, IEquatable<Short2>
{
    public Short2(float x, float y)
    {
        PackedValue = Pack(x, y);
    }

    public Short2(Vector2 vector)
    {
        PackedValue = Pack(vector.X, vector.Y);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y);

    public readonly Vector4 ToVector4() => new Vector4((short)(PackedValue & 0xFFFF), (short)((PackedValue >> 16) & 0xFFFF), 0f, 1f);

    public readonly Vector2 ToVector2() => new((short)(PackedValue & 0xFFFF), (short)((PackedValue >> 16) & 0xFFFF));

    private static uint Pack(float x, float y)
    {
        uint xi = PackUtils.PackSigned(65535u, x);
        uint yi = PackUtils.PackSigned(65535u, y);
        return xi | (yi << 16);
    }

    public readonly bool Equals(Short2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Short2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(Short2 a, Short2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Short2 a, Short2 b) => a.PackedValue != b.PackedValue;
}
