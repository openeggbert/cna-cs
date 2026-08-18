namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>DynamicVertexBuffer</c>: a vertex buffer optimised for frequent rewriting
/// from the CPU.
///
/// A thin subclass rather than its own binding, because that is what the ABI models: the same
/// <c>cna_vertex_buffer_create</c> takes a <c>dynamic</c> flag in its create-info
/// (<c>vertex_resources.h</c>), so a dynamic buffer is the identical native resource with one bit
/// set -- not a separate type. Everything else (<c>SetData</c>, <c>GetData</c>, disposal) is
/// inherited unchanged and needs no override.
///
/// <see cref="IsContentLost"/> is always <see langword="false"/> here. Real XNA raises
/// <c>ContentLost</c> when a device reset discards buffer contents, a Direct3D 9-era concept the
/// C API has no counterpart for -- rather than omit the member (XNA source reads it) or invent a
/// value, it reports the honest answer for a backend that never loses content this way.
/// </summary>
public class DynamicVertexBuffer : VertexBuffer
{
    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)
    {
    }

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, dynamic: true)
    {
    }

    /// <summary>See this class's own doc comment: no device-reset content loss exists in this
    /// backend, so this is always <see langword="false"/> rather than absent or
    /// guessed.</summary>
    public bool IsContentLost => false;

    /// <summary>Never raised -- see <see cref="IsContentLost"/>. Present so XNA source that
    /// subscribes still compiles.</summary>
    public event EventHandler<EventArgs>? ContentLost
    {
        add { }
        remove { }
    }
}
