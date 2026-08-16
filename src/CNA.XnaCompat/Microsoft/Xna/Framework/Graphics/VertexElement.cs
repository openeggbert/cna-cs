namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>VertexElement</c>. Fields are duplicated (same rationale as
/// Vector3.cs/Color.cs), converting through implicit operators -- the enum fields need an
/// explicit numeric cast at the boundary, same as every other enum crossing this project's
/// CNA/XnaCompat split.</summary>
public struct VertexElement : IEquatable<VertexElement>
{
    public int Offset;
    public VertexElementFormat VertexElementFormat;
    public VertexElementUsage VertexElementUsage;
    public int UsageIndex;

    public VertexElement(int offset, VertexElementFormat elementFormat, VertexElementUsage elementUsage, int usageIndex)
    {
        Offset = offset;
        VertexElementFormat = elementFormat;
        VertexElementUsage = elementUsage;
        UsageIndex = usageIndex;
    }

    public static bool operator ==(VertexElement a, VertexElement b) => a.Equals(b);
    public static bool operator !=(VertexElement a, VertexElement b) => !a.Equals(b);

    public readonly bool Equals(VertexElement other) =>
        Offset == other.Offset && VertexElementFormat == other.VertexElementFormat &&
        VertexElementUsage == other.VertexElementUsage && UsageIndex == other.UsageIndex;
    public override readonly bool Equals(object? obj) => obj is VertexElement other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Offset, VertexElementFormat, VertexElementUsage, UsageIndex);
    public override readonly string ToString() =>
        $"{{Offset:{Offset} Format:{VertexElementFormat} Usage:{VertexElementUsage} UsageIndex:{UsageIndex}}}";

    public static implicit operator CNA.Graphics.VertexElement(VertexElement value) => new(
        value.Offset,
        (CNA.Graphics.VertexElementFormat)(int)value.VertexElementFormat,
        (CNA.Graphics.VertexElementUsage)(int)value.VertexElementUsage,
        value.UsageIndex);

    public static implicit operator VertexElement(CNA.Graphics.VertexElement value) => new(
        value.Offset,
        (VertexElementFormat)(int)value.VertexElementFormat,
        (VertexElementUsage)(int)value.VertexElementUsage,
        value.UsageIndex);
}
