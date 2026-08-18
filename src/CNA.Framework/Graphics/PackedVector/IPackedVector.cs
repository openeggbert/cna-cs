namespace CNA.Graphics.PackedVector;

/// <summary>
/// Matches real XNA's <c>IPackedVector</c>: the untyped half of the packed-vector contract, so code
/// can round-trip any packed format through <see cref="Vector4"/> without knowing which one it has.
/// </summary>
public interface IPackedVector
{
    void PackFromVector4(Vector4 vector);

    Vector4 ToVector4();
}

/// <summary>
/// Matches real XNA's <c>IPackedVector&lt;TPacked&gt;</c>: adds direct access to the underlying
/// bits. <typeparamref name="TPacked"/> is the storage word -- <see cref="byte"/>,
/// <see cref="ushort"/>, <see cref="uint"/> or <see cref="ulong"/> depending on the format.
/// </summary>
public interface IPackedVector<TPacked> : IPackedVector
{
    TPacked PackedValue { get; set; }
}
