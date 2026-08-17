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

        byte[] data = reader.ReadExactBytes(dataSize);
        return new XnbIndexBufferData(sixteenBits, data);
    }
}
