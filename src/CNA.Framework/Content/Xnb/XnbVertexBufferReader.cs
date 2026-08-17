using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>VertexBufferReader</c> object graph, matching the real
/// openeggbert/cna C++ engine's own <c>ModelContentTypeReaders.cpp</c> exactly: a raw
/// <see cref="XnbVertexDeclarationReader"/> read (no leading dispatch byte of its own -- see that
/// type's own doc comment), a vertex count, then that many vertices' worth of raw bytes.
/// </summary>
internal static class XnbVertexBufferReader
{
    internal static object Read(XnbContentReader reader)
    {
        VertexDeclaration declaration = XnbVertexDeclarationReader.Read(reader);
        uint rawVertexCount = reader.ReadUInt32();
        if (rawVertexCount > int.MaxValue)
        {
            throw new ContentLoadException($"Corrupt .xnb file: vertex count {rawVertexCount} does not fit in a signed 32-bit count.");
        }

        int vertexCount = (int)rawVertexCount;

        long byteCount = (long)vertexCount * declaration.VertexStride;
        if (byteCount > int.MaxValue)
        {
            throw new ContentLoadException($"Corrupt .xnb file: vertex buffer of {byteCount} bytes is too large.");
        }

        byte[] data = reader.ReadExactBytes((int)byteCount);
        return new XnbVertexBufferData(declaration, vertexCount, data);
    }
}
