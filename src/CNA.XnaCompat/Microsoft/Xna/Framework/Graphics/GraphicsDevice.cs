namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>GraphicsDevice</c>. A pure subclass -- <c>Clear(Color)</c> is inherited
/// unchanged from <see cref="CNA.Graphics.GraphicsDevice"/> and resolves correctly
/// against this namespace's <see cref="Color"/> argument through that struct's implicit
/// conversion operator, so no override is needed here. See docs/architecture.md.
/// </summary>
public class GraphicsDevice : CNA.Graphics.GraphicsDevice
{
    private IndexBuffer? _indices;

    protected internal GraphicsDevice(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    /// <summary><c>SetVertexBuffer</c> is inherited unchanged (its <c>VertexBuffer</c> argument
    /// upcasts, same as every other compat method taking a native-backed resource type).
    /// <c>Indices</c> needs a `new` override since its declared type
    /// (<see cref="Microsoft.Xna.Framework.Graphics.IndexBuffer"/>) differs from the base
    /// property's.</summary>
    public new IndexBuffer? Indices
    {
        get => _indices;
        set
        {
            base.Indices = value;
            _indices = value;
        }
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount) =>
        base.DrawPrimitives((CNA.Graphics.PrimitiveType)(int)primitiveType, startVertex, primitiveCount);

    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount) =>
        base.DrawIndexedPrimitives(
            (CNA.Graphics.PrimitiveType)(int)primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);
}
