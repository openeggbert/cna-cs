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

        try
        {
            using var containerReader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            XnaHeader header = ReadHeader(containerReader, stream, assetName);

            Stream payload = stream;
            if (header.IsCompressed)
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

                payload = new MemoryStream(
                    XnbLzxDecompression.Decompress(compressed, decompressedSize, assetName), writable: false);
            }

            using (payload)
            {
                using var reader = new ContentReader(
                    contentManager, payload, assetName, version: 5, platform: 'w', recordDisposableObject);
                return reader.ReadAsset<T>(new ContentTypeReaderManager());
            }
        }
        catch (IOException exception)
        {
            // XNA normalizes truncated headers, manifests, and object bodies to
            // ContentLoadException while preserving the IOException as InnerException.
            throw new ContentLoadException($"Error loading '{assetName}'. The XNB file is invalid.", exception);
        }
    }

    /// <summary>
    /// Reads the Windows XNA 4.0 container prologue. This is intentionally stricter than CNA's
    /// cross-platform <see cref="XnbHeader"/> parser: Microsoft's Windows runtime accepts only the
    /// <c>'w'</c>/version-5 profile, treats bits 8..14 as a graphics-profile value (not MonoGame's
    /// LZ4 extension), and rejects only a declared size that runs past the available stream. A
    /// smaller declared size does not fence an uncompressed stream in XNA.
    /// </summary>
    private static XnaHeader ReadHeader(BinaryReader reader, Stream stream, string assetName)
    {
        if (reader.ReadByte() != (byte)'X' ||
            reader.ReadByte() != (byte)'N' ||
            reader.ReadByte() != (byte)'B')
        {
            throw new ContentLoadException($"Error loading '{assetName}'. Invalid XNB magic bytes.");
        }

        if (reader.ReadByte() != (byte)'w')
        {
            throw new ContentLoadException($"Error loading '{assetName}'. Invalid XNB platform.");
        }

        ushort versionAndProfile = reader.ReadUInt16();
        int format = versionAndProfile & 0x80ff;
        bool compressed = format switch
        {
            5 => false,
            0x8005 => true,
            _ => throw new ContentLoadException($"Error loading '{assetName}'. Invalid XNB version."),
        };

        int totalLength = reader.ReadInt32();
        if (stream.CanSeek && totalLength - 10 > stream.Length - stream.Position)
        {
            throw new ContentLoadException($"Error loading '{assetName}'. The XNB file is truncated.");
        }

        return new XnaHeader(compressed, totalLength);
    }

    private readonly record struct XnaHeader(bool IsCompressed, int TotalLength);
}
