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
public class VertexDeclaration : GraphicsResource
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

    /// <summary>
    /// This namespace's own equivalent of <see cref="CNA.Graphics.VertexDeclaration.FromType"/> --
    /// needed as a genuinely separate implementation, not a forwarding call, because a
    /// compat-namespaced vertex struct (e.g. <see cref="VertexPositionColor"/>) implements *this*
    /// namespace's <see cref="IVertexType"/>, a distinct interface from
    /// <c>CNA.Graphics.IVertexType</c> -- the base layer's <c>FromType</c> would never match it via
    /// its own pattern match. <c>internal</c>, matching real XNA's own accessibility (same as the
    /// base layer's).
    /// </summary>
    internal static VertexDeclaration FromType(Type vertexType)
    {
        ArgumentNullException.ThrowIfNull(vertexType);

        if (!vertexType.IsValueType)
        {
            throw new ArgumentException("vertexType must be a value type.", nameof(vertexType));
        }

        if (Activator.CreateInstance(vertexType) is not IVertexType instance)
        {
            throw new ArgumentException("vertexType does not inherit IVertexType.", nameof(vertexType));
        }

        return instance.VertexDeclaration;
    }

    /// <summary>The wrapped <c>CNA.Graphics.VertexDeclaration</c> -- <c>internal</c> (same
    /// assembly, no cross-assembly grant needed) so <c>VertexBuffer</c>'s constructor can forward
    /// it to <c>CNA.Graphics.VertexBuffer</c>'s base constructor without re-converting the
    /// element array a second time.</summary>
    internal CNA.Graphics.VertexDeclaration Framework => _framework;

    public VertexElement[] GetVertexElements()
    {
        CNA.Graphics.VertexElement[] source = _framework.GetVertexElements();
        var result = new VertexElement[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = VertexElement.FromFramework(source[i]);
        }

        return result;
    }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);

    private static CNA.Graphics.VertexElement[] ToFramework(VertexElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var result = new CNA.Graphics.VertexElement[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            result[i] = elements[i].ToFramework();
        }

        return result;
    }
}
