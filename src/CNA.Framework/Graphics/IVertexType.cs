namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>IVertexType</c> exactly -- lets
/// <see cref="VertexDeclaration.FromType"/> discover a vertex struct's layout generically for
/// <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>'s <c>Type</c>-taking constructors.
/// </summary>
public interface IVertexType
{
    VertexDeclaration VertexDeclaration { get; }
}
