namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Matches real XNA's <c>VertexPositionColor</c> exactly (layout: <c>Position</c> at
/// offset 0, <c>Color</c> at offset 12; stride 16).</summary>
public struct VertexPositionColor : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    public Vector3 Position;
    public Color Color;

    public VertexPositionColor(Vector3 position, Color color)
    {
        Position = position;
        Color = color;
    }

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public static bool operator ==(VertexPositionColor left, VertexPositionColor right) =>
        left.Position == right.Position && left.Color == right.Color;

    public static bool operator !=(VertexPositionColor left, VertexPositionColor right) => !(left == right);

    public override readonly bool Equals(object? obj) => obj is VertexPositionColor other && this == other;
    public override readonly int GetHashCode() => HashCode.Combine(Position, Color);
    public override readonly string ToString() => $"{{Position:{Position} Color:{Color}}}";
}
