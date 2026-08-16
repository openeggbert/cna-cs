namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>IndexBuffer</c>. <c>SetData</c>/<c>GetData</c>/<c>IndexCount</c>/
/// <c>Dispose</c> are inherited unchanged from <see cref="CNA.Graphics.IndexBuffer"/> for the
/// same reason <see cref="VertexBuffer"/>'s equivalent members are. <c>IndexElementSize</c> and
/// <c>BufferUsage</c> need `new` overrides.
/// </summary>
public class IndexBuffer : CNA.Graphics.IndexBuffer
{
    public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, (CNA.Graphics.IndexElementSize)(int)indexElementSize, indexCount, (CNA.Graphics.BufferUsage)(int)bufferUsage)
    {
    }

    public new IndexElementSize IndexElementSize => (IndexElementSize)(int)base.IndexElementSize;

    public new BufferUsage BufferUsage => (BufferUsage)(int)base.BufferUsage;
}
