namespace Microsoft.Xna.Framework.Graphics.PackedVector;

/// <summary>XNA 4.0-compatible <c>IPackedVector</c>. A separate declaration from
/// <c>CNA.Graphics.PackedVector.IPackedVector</c> rather than an alias, because its
/// <c>PackFromVector4</c>/<c>ToVector4</c> are typed on this namespace's own
/// <see cref="Vector4"/>.</summary>
public interface IPackedVector
{
    void PackFromVector4(Vector4 vector);

    Vector4 ToVector4();
}

/// <summary>XNA 4.0-compatible <c>IPackedVector&lt;TPacked&gt;</c>.</summary>
public interface IPackedVector<TPacked> : IPackedVector
{
    TPacked PackedValue { get; set; }
}
