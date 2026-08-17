namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>IndexBufferReader</c> object graph, matching the real
/// openeggbert/cna C++ engine's own <c>ModelContentTypeReaders.cpp</c> exactly: a
/// sixteen-vs-thirty-two-bit flag, a byte count, then that many raw index bytes.
/// </summary>
internal static class XnbIndexBufferReader
{
    internal static object Read(XnbContentReader reader)
    {
        bool sixteenBits = reader.ReadBoolean();
        int dataSize = reader.ReadInt32();
        if (dataSize < 0)
        {
            throw new ContentLoadException($"Corrupt .xnb file: negative index buffer size {dataSize}.");
        }

        // A code-review finding: element count is later derived as dataSize / elementSize
        // (XnbModelBuilder.BuildIndexBuffer) -- if dataSize isn't a whole number of elements,
        // that division truncates, so the real IndexBuffer would be allocated smaller than the
        // byte array actually written into it. Reject here, at the source of the corrupt data,
        // rather than risk an out-of-bounds native write downstream.
        int elementSize = sixteenBits ? 2 : 4;
        if (dataSize % elementSize != 0)
        {
            throw new ContentLoadException(
                $"Corrupt .xnb file: index buffer size {dataSize} is not a whole number of {elementSize}-byte indices.");
        }

        byte[] data = reader.ReadExactBytes(dataSize);
        return new XnbIndexBufferData(sixteenBits, data);
    }
}
