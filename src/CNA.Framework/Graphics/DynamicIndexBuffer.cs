namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DynamicIndexBuffer</c>. See
/// <see cref="DynamicVertexBuffer"/>'s own doc comment -- identical rationale, including why
/// <see cref="IsContentLost"/> is always <see langword="false"/>.</summary>
public class DynamicIndexBuffer : IndexBuffer
{
    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, SizeForType(indexType), indexCount, bufferUsage)
    {
    }

    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, indexElementSize, indexCount, bufferUsage, dynamic: true)
    {
    }

    public bool IsContentLost => false;

    public event EventHandler<EventArgs>? ContentLost
    {
        add { }
        remove { }
    }
}
