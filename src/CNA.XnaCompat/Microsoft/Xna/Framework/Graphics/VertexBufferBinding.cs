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

    /// <summary>
    /// The inverse, for <see cref="GraphicsDevice.GetVertexBuffers"/>.
    ///
    /// The buffer has to be a compat <see cref="VertexBuffer"/>, and it will be: the only way a
    /// binding reaches the base's cache is through this namespace's <c>SetVertexBuffer(s)</c>, so
    /// what comes back is what a compat game put in. A cast failure would mean the base handed back
    /// a binding it was never given, which is worth surfacing rather than swallowing.
    /// </summary>
    internal static VertexBufferBinding FromFramework(CNA.Graphics.VertexBufferBinding binding) =>
        new(
            binding.VertexBuffer as VertexBuffer
            ?? throw new InvalidOperationException(
                "A vertex-buffer binding came back holding a CNA.Graphics.VertexBuffer that is not " +
                "this namespace's own, which means it was bound outside Microsoft.Xna.Framework."),
            binding.VertexOffset,
            binding.InstanceFrequency);
}
