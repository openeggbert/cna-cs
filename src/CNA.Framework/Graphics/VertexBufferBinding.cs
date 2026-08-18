using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>VertexBufferBinding</c>: one stream of a multi-stream vertex
/// setup, as passed to <see cref="GraphicsDevice.SetVertexBuffers"/>.
/// <see cref="InstanceFrequency"/> of zero means per-vertex data; a positive value means the
/// stream advances once per that many instances, which is how hardware instancing is
/// expressed.</summary>
public readonly struct VertexBufferBinding
{
    public VertexBufferBinding(VertexBuffer vertexBuffer)
        : this(vertexBuffer, 0, 0)
    {
    }

    public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset)
        : this(vertexBuffer, vertexOffset, 0)
    {
    }

    public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset, int instanceFrequency)
    {
        ArgumentNullException.ThrowIfNull(vertexBuffer);
        ArgumentOutOfRangeException.ThrowIfNegative(vertexOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(instanceFrequency);

        VertexBuffer = vertexBuffer;
        VertexOffset = vertexOffset;
        InstanceFrequency = instanceFrequency;
    }

    public VertexBuffer VertexBuffer { get; }

    public int VertexOffset { get; }

    public int InstanceFrequency { get; }

    internal CnaVertexBufferBinding ToNative() => new()
    {
        VertexBuffer = new CnaHandle(VertexBuffer.NativeHandleValue),
        VertexOffset = VertexOffset,
        InstanceFrequency = InstanceFrequency,
    };
}
