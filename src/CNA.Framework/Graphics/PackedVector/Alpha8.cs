namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Alpha8</c>: 8-bit normalized alpha. Only the W component participates; <see cref="ToVector4"/> reports the other three as zero.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Alpha8.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Alpha8 : IPackedVector<byte>, IEquatable<Alpha8>
{
    public Alpha8(float alpha)
    {
        PackedValue = Pack(alpha);
    }


    /// <summary>The raw storage word.</summary>
    public byte PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.W);

    public readonly Vector4 ToVector4() => new Vector4(0f, 0f, 0f, PackedValue / 255f);

    /// <summary>The alpha channel expanded back to [0, 1].</summary>
    public readonly float ToAlpha() => PackedValue / 255f;

    private static byte Pack(float alpha)
        => (byte)(Math.Clamp(alpha, 0f, 1f) * 255f + 0.5f);

    public readonly bool Equals(Alpha8 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Alpha8 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Alpha8 a, Alpha8 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Alpha8 a, Alpha8 b) => a.PackedValue != b.PackedValue;
}
