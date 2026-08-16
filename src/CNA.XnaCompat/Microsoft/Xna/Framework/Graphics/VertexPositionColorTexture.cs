namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Matches real XNA's <c>VertexPositionColorTexture</c> exactly (layout: <c>Position</c>
/// at offset 0, <c>Color</c> at offset 12, <c>TextureCoordinate</c> at offset 16; stride 24).
/// </summary>
public struct VertexPositionColorTexture : IVertexType, IEquatable<VertexPositionColorTexture>
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    public Vector3 Position;
    public Color Color;
    public Vector2 TextureCoordinate;

    public VertexPositionColorTexture(Vector3 position, Color color, Vector2 textureCoordinate)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
    }

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public static bool operator ==(VertexPositionColorTexture a, VertexPositionColorTexture b) => a.Equals(b);
    public static bool operator !=(VertexPositionColorTexture a, VertexPositionColorTexture b) => !a.Equals(b);

    public readonly bool Equals(VertexPositionColorTexture other) =>
        Position.Equals(other.Position) && Color.Equals(other.Color) && TextureCoordinate.Equals(other.TextureCoordinate);
    public override readonly bool Equals(object? obj) => obj is VertexPositionColorTexture other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, Color, TextureCoordinate);
    public override readonly string ToString() =>
        $"{{Position:{Position} Color:{Color} TextureCoordinate:{TextureCoordinate}}}";
}
