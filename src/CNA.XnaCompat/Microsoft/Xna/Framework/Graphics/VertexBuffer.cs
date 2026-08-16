namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>VertexBuffer</c>. <c>SetData</c>/<c>GetData</c>/<c>VertexCount</c>/
/// <c>Dispose</c> are inherited unchanged from <see cref="CNA.Graphics.VertexBuffer"/> --
/// <c>SetData&lt;T&gt;</c>/<c>GetData&lt;T&gt;</c> operate purely on the caller's own generic
/// type parameter, so no compat-type crossing happens inside them at all. <c>VertexDeclaration</c>
/// and <c>BufferUsage</c> both need `new` overrides, same reason <c>SoundEffectInstance.State</c>
/// and <c>VertexBuffer</c>'s sibling types elsewhere in this codebase do.
/// </summary>
public class VertexBuffer : CNA.Graphics.VertexBuffer
{
    private readonly VertexDeclaration _vertexDeclaration;

    public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
        : base(graphicsDevice, ToFramework(vertexDeclaration), vertexCount, (CNA.Graphics.BufferUsage)(int)bufferUsage)
    {
        _vertexDeclaration = vertexDeclaration;
    }

    public new VertexDeclaration VertexDeclaration => _vertexDeclaration;

    public new BufferUsage BufferUsage => (BufferUsage)(int)base.BufferUsage;

    private static CNA.Graphics.VertexDeclaration ToFramework(VertexDeclaration vertexDeclaration)
    {
        ArgumentNullException.ThrowIfNull(vertexDeclaration);
        return vertexDeclaration.Framework;
    }
}
