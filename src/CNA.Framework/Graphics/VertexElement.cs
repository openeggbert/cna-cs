namespace CNA.Graphics;

/// <summary>Describes one field within a vertex's byte layout -- matches real XNA's
/// <c>VertexElement</c> exactly. Pure data, no native dependency (mirrors the real
/// openeggbert/cna C++ engine's own <c>VertexElement</c>, which likewise just stores
/// offset/format/usage/usageIndex fields -- see NEXT.md).</summary>
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
}
