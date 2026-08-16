namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>VertexPosition</c> exactly (layout: <c>Position</c> at offset 0).</summary>
public struct VertexPosition : IVertexType, IEquatable<VertexPosition>
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));

    public Vector3 Position;

    public VertexPosition(Vector3 position)
    {
        Position = position;
    }

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public static bool operator ==(VertexPosition a, VertexPosition b) => a.Equals(b);
    public static bool operator !=(VertexPosition a, VertexPosition b) => !a.Equals(b);

    public readonly bool Equals(VertexPosition other) => Position.Equals(other.Position);
    public override readonly bool Equals(object? obj) => obj is VertexPosition other && Equals(other);
    public override readonly int GetHashCode() => Position.GetHashCode();
    public override readonly string ToString() => $"{{Position:{Position}}}";
}
