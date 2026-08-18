namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>VertexBufferBinding</c>. Duplicates rather than subclasses
/// <see cref="CNA.Graphics.VertexBufferBinding"/> (structs cannot inherit); it converts to the
/// base form when handed to <see cref="GraphicsDevice.SetVertexBuffers"/>.</summary>
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

    internal CNA.Graphics.VertexBufferBinding ToFramework() =>
        new(VertexBuffer, VertexOffset, InstanceFrequency);
}
