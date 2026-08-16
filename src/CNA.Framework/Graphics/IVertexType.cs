namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>IVertexType</c> exactly -- lets <c>VertexBuffer.SetData&lt;T&gt;</c>
/// (Phase 4, native-backed, not implemented yet) discover a vertex struct's layout generically.
/// </summary>
public interface IVertexType
{
    VertexDeclaration VertexDeclaration { get; }
}
