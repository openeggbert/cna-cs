namespace CNA.Graphics;

/// <summary>
/// A vertex layout: byte stride plus a set of <see cref="VertexElement"/>s. Matches real XNA's
/// <c>VertexDeclaration</c> exactly, including its stride-auto-computed-from-elements
/// constructor. Pure data/arithmetic, no native dependency -- confirmed against the real
/// openeggbert/cna C++ engine's own <c>VertexDeclaration</c> implementation, which likewise
/// "auto-computes stride from element offsets/formats" natively rather than needing the GPU (see
/// NEXT.md); this is the same "escape hatch" pattern <c>SpriteFont</c> found for its own
/// construction.
/// </summary>
public class VertexDeclaration
{
    private readonly VertexElement[] _elements;

    public VertexDeclaration(params VertexElement[] elements)
        : this(ComputeStride(elements), elements)
    {
    }

    public VertexDeclaration(int vertexStride, params VertexElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexStride);

        _elements = (VertexElement[])elements.Clone();
        VertexStride = vertexStride;
    }

    public int VertexStride { get; }

    public VertexElement[] GetVertexElements() => (VertexElement[])_elements.Clone();

    /// <summary>Stride is the tightest span covering every element (max of offset + that
    /// element's own byte size), matching real XNA's own auto-stride behavior for the
    /// elements-only constructor -- not simply the sum of element sizes, since elements are not
    /// required to be contiguous or given in offset order.</summary>
    private static int ComputeStride(VertexElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        int stride = 0;
        foreach (VertexElement element in elements)
        {
            int end = element.Offset + GetTypeSize(element.VertexElementFormat);
            if (end > stride)
            {
                stride = end;
            }
        }

        return stride;
    }

    public static int GetTypeSize(VertexElementFormat elementFormat) => elementFormat switch
    {
        VertexElementFormat.Single => 4,
        VertexElementFormat.Vector2 => 8,
        VertexElementFormat.Vector3 => 12,
        VertexElementFormat.Vector4 => 16,
        VertexElementFormat.Color => 4,
        VertexElementFormat.Byte4 => 4,
        VertexElementFormat.Short2 => 4,
        VertexElementFormat.Short4 => 8,
        VertexElementFormat.NormalizedShort2 => 4,
        VertexElementFormat.NormalizedShort4 => 8,
        VertexElementFormat.HalfVector2 => 4,
        VertexElementFormat.HalfVector4 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(elementFormat), elementFormat, null),
    };
}
