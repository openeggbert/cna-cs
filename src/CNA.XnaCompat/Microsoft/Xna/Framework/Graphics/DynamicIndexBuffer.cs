namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DynamicIndexBuffer</c>. See <see cref="DynamicVertexBuffer"/>
/// for the pattern.</summary>
public class DynamicIndexBuffer : CNA.Graphics.DynamicIndexBuffer
{
    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, CNA.Graphics.IndexBuffer.SizeForType(indexType), indexCount, (CNA.Graphics.BufferUsage)(int)bufferUsage)
    {
    }

    public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, (CNA.Graphics.IndexElementSize)(int)indexElementSize, indexCount, (CNA.Graphics.BufferUsage)(int)bufferUsage)
    {
    }
}
