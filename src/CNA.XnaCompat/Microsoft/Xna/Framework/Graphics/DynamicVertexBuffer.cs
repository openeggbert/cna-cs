namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DynamicVertexBuffer</c>. Mirrors <see cref="VertexBuffer"/>'s
/// own structure exactly, including keeping its own compat-typed
/// <see cref="VertexDeclaration"/> field -- see that type's doc comment for why the declaration
/// cannot simply be downcast from the base.</summary>
public class DynamicVertexBuffer : CNA.Graphics.DynamicVertexBuffer
{
    private readonly VertexDeclaration _vertexDeclaration;

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage bufferUsage)
        : this(graphicsDevice, VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)
    {
    }

    public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage)
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
