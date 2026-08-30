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
            Stream payload = OpenPayload(stream, assetName);
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
    /// Reads the container prologue and hands back the stream positioned at the type-reader table,
    /// decompressing first when the header says the payload is compressed.
    ///
    /// Split out so <c>tools/content-survey</c> can reach the reader table of a compressed asset
    /// through the loader's own code rather than a second copy of the container format. The survey
    /// skipped compressed assets before that, which quietly excluded every LZX-compressed font and
    /// texture from the number it reported.
    /// </summary>
    internal static Stream OpenPayload(Stream stream, string assetName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(assetName);

        using var containerReader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        XnaHeader header = ReadHeader(containerReader, stream, assetName);

        if (!header.IsCompressed)
        {
            return stream;
        }

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

        return new MemoryStream(
            XnbLzxDecompression.Decompress(compressed, decompressedSize, assetName), writable: false);
    }

    /// <summary>
    /// Reads the XNA 4.0 container prologue. This follows Microsoft's Windows runtime in treating
    /// bits 8..14 as a graphics-profile value (not MonoGame's LZ4 extension) and in rejecting only
    /// a declared size that runs past the available stream -- a smaller declared size does not
    /// fence an uncompressed stream in XNA.
    ///
    /// It is deliberately wider than XNA in one place: the platform byte. XNA on Windows accepts
    /// only <c>'w'</c> and refuses content compiled for Windows Phone (<c>'m'</c>) or Xbox 360
    /// (<c>'x'</c>). A game ported to the desktop routinely ships the assets it was originally
    /// built with, and three of the XNA 4.0 sample collection's own content folders are
    /// <c>'m'</c> -- refusing them means refusing the game over a byte that describes which
    /// pipeline ran, not how the payload is encoded. The payload is little-endian in all three
    /// cases; what genuinely differs is which surface formats the pipeline chose, and an
    /// unsupported format fails later with a message about the format.
    /// </summary>
    private static XnaHeader ReadHeader(BinaryReader reader, Stream stream, string assetName)
    {
        if (reader.ReadByte() != (byte)'X' ||
            reader.ReadByte() != (byte)'N' ||
            reader.ReadByte() != (byte)'B')
        {
            throw new ContentLoadException($"Error loading '{assetName}'. Invalid XNB magic bytes.");
        }

        byte platform = reader.ReadByte();
        if (platform is not ((byte)'w' or (byte)'m' or (byte)'x'))
        {
            throw new ContentLoadException(
                $"Error loading '{assetName}'. Invalid XNB platform '{(char)platform}'; " +
                "expected 'w' (Windows), 'm' (Windows Phone) or 'x' (Xbox 360).");
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
