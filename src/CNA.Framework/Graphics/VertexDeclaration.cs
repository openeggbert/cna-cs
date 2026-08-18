using CNA.Interop;
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
        : this(ComputeStride(ValidateNotEmpty(elements)), elements)
    {
    }

    public VertexDeclaration(int vertexStride, params VertexElement[] elements)
    {
        ValidateNotEmpty(elements);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexStride);

        _elements = (VertexElement[])elements.Clone();
        VertexStride = vertexStride;
    }

    public int VertexStride { get; }

    public VertexElement[] GetVertexElements() => (VertexElement[])_elements.Clone();

    /// <summary>
    /// Builds a native <c>CNA_VertexDeclarationHandle</c> from this declaration's own elements and
    /// stride. The caller owns the result and must destroy it.
    ///
    /// This type stays a pure C# data type, matching real XNA's object model -- only the call sites
    /// that hand a declaration to native need a handle, and only momentarily. It lives here rather
    /// than in <see cref="VertexBuffer"/>, where it started, because
    /// <see cref="GraphicsDevice.DrawUserPrimitives{T}"/> needs the same conversion: without it,
    /// that method could only draw the four vertex types <c>CNA_UserVertexSource</c> names by
    /// identity, which was a self-imposed limit rather than an ABI one.
    ///
    /// <see cref="VertexElement"/>'s field order and <see cref="VertexElementFormat"/>/
    /// <see cref="VertexElementUsage"/>'s numeric values were confirmed to match
    /// <c>CNA_VertexElement</c>/<c>CNA_VERTEX_ELEMENT_FORMAT_*</c>/<c>CNA_VERTEX_ELEMENT_USAGE_*</c>
    /// exactly before relying on the direct field-for-field conversion.
    /// </summary>
    internal unsafe CnaHandle CreateNativeHandle()
    {
        VertexElement[] elements = GetVertexElements();
        var nativeElements = new CnaVertexElement[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            nativeElements[i] = new CnaVertexElement(
                elements[i].Offset,
                (uint)elements[i].VertexElementFormat,
                (uint)elements[i].VertexElementUsage,
                elements[i].UsageIndex);
        }

        fixed (CnaVertexElement* elementsPtr = nativeElements)
        {
            CnaResult result = Native.cna_vertex_declaration_create_with_stride(
                VertexStride, elementsPtr, (ulong)nativeElements.Length, out CnaHandle declaration);
            CnaException.ThrowIfFailed(result, nameof(VertexDeclaration));
            return declaration;
        }
    }

    /// <summary>
    /// Derives a <see cref="VertexDeclaration"/> from an <see cref="IVertexType"/>-implementing
    /// value type via reflection -- matches real XNA/MonoGame's own internal
    /// <c>VertexDeclaration.FromType</c> exactly (construct a default instance, read its
    /// <see cref="IVertexType.VertexDeclaration"/> property), including its exception shape.
    /// Message text recalled from memory (MonoGame source), not independently verified against a
    /// live binary or decompiled source -- same honesty flag this session already used for
    /// <c>SpriteBatch</c>'s own <c>Begin</c>/<c>End</c> message text. <c>internal</c>, matching
    /// real XNA's own accessibility -- this is <see cref="VertexBuffer"/>'s
    /// <c>Type</c>-taking constructor's implementation detail, not standalone public API.
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

    /// <summary>Matches real XNA/MonoGame exactly: both constructors reject a null *or empty*
    /// array with <see cref="ArgumentNullException"/> (not <see cref="ArgumentException"/>, and
    /// regardless of whether an explicit stride was given) -- verified against real MonoGame
    /// source, not assumed. Returns <paramref name="elements"/> so it can be chained into the
    /// elements-only constructor's `: this(...)` initializer, which runs before that
    /// constructor's own body.</summary>
    private static VertexElement[] ValidateNotEmpty(VertexElement[] elements)
    {
        if (elements is null or { Length: 0 })
        {
            throw new ArgumentNullException(nameof(elements), "Elements cannot be empty");
        }

        return elements;
    }

    /// <summary>Stride is the tightest span covering every element (max of offset + that
    /// element's own byte size), matching real XNA's own auto-stride behavior for the
    /// elements-only constructor -- not simply the sum of element sizes, since elements are not
    /// required to be contiguous or given in offset order.</summary>
    private static int ComputeStride(VertexElement[] elements)
    {
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
