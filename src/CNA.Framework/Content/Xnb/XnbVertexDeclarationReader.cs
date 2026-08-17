using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>VertexDeclarationReader</c> object graph -- matching the real
/// openeggbert/cna C++ engine's own <c>ModelContentTypeReaders.cpp</c> exactly. Unlike every other
/// reader in this feature, this is **not** invoked via <see cref="XnbContentReader.ReadObject"/>'s
/// dispatch protocol: real XNA's own <c>VertexBufferReader</c> reads a <c>VertexDeclaration</c> as
/// a *raw* object (FNA's own <c>ReadRawObject&lt;T&gt;</c> mechanism) -- the caller already
/// statically knows the next bytes are a <c>VertexDeclaration</c> in exactly this format, so there
/// is no leading type-reader-index byte the way a normal dispatched object has.
/// </summary>
internal static class XnbVertexDeclarationReader
{
    internal static VertexDeclaration Read(XnbContentReader reader)
    {
        int vertexStride = reader.ReadInt32();
        int elementCount = reader.ReadInt32();
        if (elementCount is < 0 or > 1024)
        {
            throw new ContentLoadException($"Corrupt .xnb file: implausible vertex element count {elementCount}.");
        }

        var elements = new VertexElement[elementCount];
        for (int i = 0; i < elementCount; i++)
        {
            int offset = reader.ReadInt32();
            var format = (VertexElementFormat)reader.ReadInt32();
            var usage = (VertexElementUsage)reader.ReadInt32();
            int usageIndex = reader.ReadInt32();
            elements[i] = new VertexElement(offset, format, usage, usageIndex);
        }

        return new VertexDeclaration(vertexStride, elements);
    }
}
