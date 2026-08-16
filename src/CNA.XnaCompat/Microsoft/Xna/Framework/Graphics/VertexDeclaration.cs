namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>VertexDeclaration</c>. Wraps (composition) rather than subclasses
/// <see cref="CNA.Graphics.VertexDeclaration"/> -- there is no construction-seam reason to
/// subclass here the way <c>Texture2D</c> needs to (no "wrap an already-created native handle"
/// case; a <c>VertexDeclaration</c> is never native-backed), and wrapping sidesteps needing an
/// internal raw-element-array constructor just for this. Element-wise array conversion (not a
/// collection-level one) is needed both ways for the same reason it's needed everywhere else in
/// this codebase: arrays of a type with a user-defined conversion operator do not convert
/// automatically.
/// </summary>
public class VertexDeclaration
{
    private readonly CNA.Graphics.VertexDeclaration _framework;

    public VertexDeclaration(params VertexElement[] elements)
    {
        _framework = new CNA.Graphics.VertexDeclaration(ToFramework(elements));
    }

    public VertexDeclaration(int vertexStride, params VertexElement[] elements)
    {
        _framework = new CNA.Graphics.VertexDeclaration(vertexStride, ToFramework(elements));
    }

    public int VertexStride => _framework.VertexStride;

    public VertexElement[] GetVertexElements()
    {
        CNA.Graphics.VertexElement[] source = _framework.GetVertexElements();
        var result = new VertexElement[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i];
        }

        return result;
    }

    private static CNA.Graphics.VertexElement[] ToFramework(VertexElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var result = new CNA.Graphics.VertexElement[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            result[i] = elements[i];
        }

        return result;
    }
}
