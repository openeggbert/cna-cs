namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Matches real XNA's <c>VertexPositionTexture</c> exactly (layout: <c>Position</c> at
/// offset 0, <c>TextureCoordinate</c> at offset 12; stride 20).</summary>
public struct VertexPositionTexture : IVertexType, IEquatable<VertexPositionTexture>
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    public Vector3 Position;
    public Vector2 TextureCoordinate;

    public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
    {
        Position = position;
        TextureCoordinate = textureCoordinate;
    }

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public static bool operator ==(VertexPositionTexture a, VertexPositionTexture b) => a.Equals(b);
    public static bool operator !=(VertexPositionTexture a, VertexPositionTexture b) => !a.Equals(b);

    public readonly bool Equals(VertexPositionTexture other) =>
        Position.Equals(other.Position) && TextureCoordinate.Equals(other.TextureCoordinate);
    public override readonly bool Equals(object? obj) => obj is VertexPositionTexture other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Position, TextureCoordinate);
    public override readonly string ToString() => $"{{Position:{Position} TextureCoordinate:{TextureCoordinate}}}";
}
