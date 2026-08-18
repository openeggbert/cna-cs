namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>Bgr565</c>: 16-bit normalized BGR 5:6:5. Green gets the extra bit, which is why its divisor is 63 and the others' is 31.
///
/// Managed, not a P/Invoke. <c>packed_vectors.h</c> does expose
/// <c>cna_packed_vector_pack</c>/<c>_unpack</c> for all seventeen formats, but design invariant #3
/// keeps math value types managed -- crossing the ABI for a handful of shifts would cost more than
/// the arithmetic. The packing rules below are ported from the engine's own
/// <c>Bgr565.hpp</c> rather than reconstructed from the format name, so they agree with what native
/// would have produced, including its rounding.
/// </summary>
public struct Bgr565 : IPackedVector<ushort>, IEquatable<Bgr565>
{
    public Bgr565(float r, float g, float b)
    {
        PackedValue = Pack(r, g, b);
    }

    public Bgr565(Vector3 vector)
    {
        PackedValue = Pack(vector.X, vector.Y, vector.Z);
    }


    /// <summary>The raw storage word.</summary>
    public ushort PackedValue { get; set; }

    public void PackFromVector4(Vector4 vector) => PackedValue = Pack(vector.X, vector.Y, vector.Z);

    public readonly Vector4 ToVector4() => new Vector4(((PackedValue >> 11) & 0x1F) / 31f, ((PackedValue >> 5) & 0x3F) / 63f, (PackedValue & 0x1F) / 31f, 1f);

    /// <summary>The three channels expanded back to [0, 1].</summary>
    public readonly Vector3 ToVector3() => new(((PackedValue >> 11) & 0x1F) / 31f, ((PackedValue >> 5) & 0x3F) / 63f, (PackedValue & 0x1F) / 31f);

    private static ushort Pack(float r, float g, float b)
    {
        uint ri = (uint)(Math.Clamp(r, 0f, 1f) * 31f + 0.5f);
        uint gi = (uint)(Math.Clamp(g, 0f, 1f) * 63f + 0.5f);
        uint bi = (uint)(Math.Clamp(b, 0f, 1f) * 31f + 0.5f);
        return (ushort)((ri << 11) | (gi << 5) | bi);
    }

    public readonly bool Equals(Bgr565 other) => PackedValue == other.PackedValue;

    public override readonly bool Equals(object? obj) => obj is Bgr565 other && Equals(other);

    public override readonly int GetHashCode() => PackedValue.GetHashCode();

    public override readonly string ToString() => PackedValue.ToString();

    public static bool operator ==(Bgr565 a, Bgr565 b) => a.PackedValue == b.PackedValue;

    public static bool operator !=(Bgr565 a, Bgr565 b) => a.PackedValue != b.PackedValue;
}
