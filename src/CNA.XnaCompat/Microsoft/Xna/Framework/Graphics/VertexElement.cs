namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>VertexElement</c>. Fields are duplicated (same rationale as
/// Vector3.cs/Color.cs), converting through implicit operators -- the enum fields need an
/// explicit numeric cast at the boundary, same as every other enum crossing this project's
/// CNA/XnaCompat split.</summary>
public struct VertexElement
{
    public int Offset { get; set; }

    public VertexElementFormat VertexElementFormat { get; set; }

    public VertexElementUsage VertexElementUsage { get; set; }

    public int UsageIndex { get; set; }

    public VertexElement(int offset, VertexElementFormat elementFormat, VertexElementUsage elementUsage, int usageIndex)
    {
        Offset = offset;
        VertexElementFormat = elementFormat;
        VertexElementUsage = elementUsage;
        UsageIndex = usageIndex;
    }

    public static bool operator ==(VertexElement left, VertexElement right) =>
        left.Offset == right.Offset && left.VertexElementFormat == right.VertexElementFormat &&
        left.VertexElementUsage == right.VertexElementUsage && left.UsageIndex == right.UsageIndex;

    public static bool operator !=(VertexElement left, VertexElement right) => !(left == right);

    public override readonly bool Equals(object? obj) => obj is VertexElement other && this == other;
    public override readonly int GetHashCode() => HashCode.Combine(Offset, VertexElementFormat, VertexElementUsage, UsageIndex);
    public override readonly string ToString() =>
        $"{{Offset:{Offset} Format:{VertexElementFormat} Usage:{VertexElementUsage} UsageIndex:{UsageIndex}}}";

    internal readonly CNA.Graphics.VertexElement ToFramework() => new(
        Offset,
        (CNA.Graphics.VertexElementFormat)(int)VertexElementFormat,
        (CNA.Graphics.VertexElementUsage)(int)VertexElementUsage,
        UsageIndex);

    internal static VertexElement FromFramework(CNA.Graphics.VertexElement value) => new(
        value.Offset,
        (VertexElementFormat)(int)value.VertexElementFormat,
        (VertexElementUsage)(int)value.VertexElementUsage,
        value.UsageIndex);
}
