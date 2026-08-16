namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Matches real XNA's <c>VertexPositionNormalTexture</c> exactly (layout: <c>Position</c>
/// at offset 0, <c>Normal</c> at offset 12, <c>TextureCoordinate</c> at offset 24; stride 32).
/// </summary>
public struct VertexPositionNormalTexture : IVertexType, IEquatable<VertexPositionNormalTexture>
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinate;

    public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
    {
        Position = position;
        Normal = normal;
        TextureCoordinate = textureCoordinate;
    }

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public static bool operator ==(VertexPositionNormalTexture a, VertexPositionNormalTexture b) => a.Equals(b);
    public static bool operator !=(VertexPositionNormalTexture a, VertexPositionNormalTexture b) => !a.Equals(b);

    public readonly bool Equals(VertexPositionNormalTexture other) =>
        Position.Equals(other.Position) && Normal.Equals(other.Normal) && TextureCoordinate.Equals(other.TextureCoordinate);
    public override readonly bool Equals(object? obj) => obj is VertexPositionNormalTexture other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, Normal, TextureCoordinate);
    public override readonly string ToString() =>
        $"{{Position:{Position} Normal:{Normal} TextureCoordinate:{TextureCoordinate}}}";
}
