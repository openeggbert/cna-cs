namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>HalfVector2</c>. See <see cref="HalfSingle"/> for the conversion semantics.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>HalfVector2.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct HalfVector2 : IPackedVector<uint>, IEquatable<HalfVector2>
{
    public HalfVector2(float x, float y)
    {
        PackedValue = Pack(x, y);
    }

    public HalfVector2(Vector2 vector)
    {
        PackedValue = Pack(vector.X, vector.Y);
    }


    /// <summary>The raw storage word.</summary>
    public uint PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y);

    public readonly Vector4 ToVector4()
    {
        Vector2 value = ToVector2();
        return new Vector4(value.X, value.Y, 0f, 1f);
    }

    /// <summary>Expands both halves back to single precision.</summary>
    public readonly Vector2 ToVector2() => new(
        HalfUtils.Unpack((ushort)(PackedValue & 0xFFFF)),
        HalfUtils.Unpack((ushort)(PackedValue >> 16)));

    private static uint Pack(float x, float y)
        => HalfUtils.Pack(x) | ((uint)HalfUtils.Pack(y) << 16);

    public readonly bool Equals(HalfVector2 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is HalfVector2 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(HalfVector2 a, HalfVector2 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(HalfVector2 a, HalfVector2 b) => a.PackedValue != b.PackedValue;
}
