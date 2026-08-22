using CNA.Content.Xnb;

namespace Microsoft.Xna.Framework.Content;

/// <summary>Managed XNB dispatch for user supplied XNA content readers.</summary>
internal static class ManagedXnbContentLoader
{
    internal static T Load<T>(
        ContentManager contentManager,
        Stream stream,
        string assetName,
        Action<IDisposable>? recordDisposableObject)
    {
        ArgumentNullException.ThrowIfNull(contentManager);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(assetName);

        using var containerReader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        XnbHeader header = XnbHeader.Read(containerReader, stream.Length);

        Stream payload = stream;
        if (header.Compression == XnbCompression.Lzx)
        {
            if (header.TotalLength < XnbHeader.LzxPayloadOffset)
            {
                throw new ContentLoadException(
                    $"'{assetName}' is not a valid LZX-compressed XNB file (payload header is truncated).");
            }

            int decompressedSize = containerReader.ReadInt32();
            int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
            byte[] compressed = containerReader.ReadBytes(compressedSize);
            if (compressed.Length != compressedSize)
            {
                throw new ContentLoadException($"'{assetName}' is a truncated LZX-compressed XNB file.");
            }

            payload = new MemoryStream(XnbLzxDecompression.Decompress(compressed, decompressedSize, assetName), writable: false);
        }

        using (payload)
        {
            using var reader = new ContentReader(
                contentManager, payload, assetName, header.Version, header.Platform, recordDisposableObject);
            return reader.ReadAsset<T>(new ContentTypeReaderManager());
        }
    }
}
