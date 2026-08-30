namespace CNA.Content.Xnb;

/// <summary>
/// Opens the <c>.xnb</c> container -- header, optional LZX payload, type-reader table -- and hands
/// a positioned <see cref="XnbContentReader"/> to a caller that decides what to do with it.
///
/// Extracted because there are now two callers with different questions. <c>ContentManager</c> reads
/// the whole object graph; <see cref="RootReaderName"/> reads only the root's type-reader index, to
/// answer "what *is* this asset" for an external reference whose type the referring file does not
/// state. Both need the identical container handling, including the LZX path and its length checks,
/// and a second copy of that is how the two would come to disagree about a malformed file.
/// </summary>
internal static class XnbContainer
{
    /// <summary>Opens <paramref name="path"/> and invokes <paramref name="read"/> with a reader
    /// positioned at the root object. The streams are closed before this returns, so nothing
    /// <paramref name="read"/> produces may reference them.</summary>
    internal static T Read<T>(string path, string assetName, Func<XnbContentReader, T> read)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(assetName);
        ArgumentNullException.ThrowIfNull(read);

        using FileStream stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        XnbHeader header = XnbHeader.Read(reader, stream.Length);

        if (header.Compression != XnbCompression.Lzx)
        {
            return read(XnbContentReader.Create(reader, assetName));
        }

        // header.TotalLength is checked against the actual stream length in XnbHeader.Read, never
        // against XnbHeader.LzxPayloadOffset. A file whose header claims 10-13 bytes total -- too
        // short to hold the 4-byte decompressed-size field an Lzx-flagged file must follow with --
        // would otherwise reach ReadInt32() below with fewer than 4 bytes left and throw
        // EndOfStreamException instead of this project's ContentLoadException contract for corrupt
        // content. Checked before that read rather than after: reaching a later
        // `compressedSize < 0` check would already imply the length was sufficient, so such a check
        // would be unreachable.
        if (header.TotalLength < XnbHeader.LzxPayloadOffset)
        {
            throw new ContentLoadException(
                $"'{assetName}' is not a valid LZX-compressed .xnb file (its declared total length is too short to hold a compressed payload).");
        }

        int decompressedSize = reader.ReadInt32();
        int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
        byte[] compressed = reader.ReadBytes(compressedSize);
        byte[] decompressed = XnbLzxDecompression.Decompress(compressed, decompressedSize, assetName);

        using var payload = new BinaryReader(new MemoryStream(decompressed));
        return read(XnbContentReader.Create(payload, assetName));
    }

    /// <summary>
    /// The name of the type reader an asset's root object dispatches to, or <see langword="null"/>
    /// when the root object is null.
    ///
    /// This is what <c>ContentReader.ReadExternalReference&lt;object&gt;</c> needs and the referring
    /// file cannot supply: an <c>ExternalReference</c> inside an <c>EffectMaterial</c>'s parameter
    /// dictionary names an asset and says nothing about its type, and XNA answered by calling
    /// <c>Load&lt;object&gt;</c> -- which dispatches on what the referenced file itself declares.
    /// Reading the file's own answer is the same question asked the same way.
    ///
    /// It costs a second open, and for a compressed asset a second decompress, of a file the caller
    /// is about to load anyway. That is accepted rather than optimised: the alternative is guessing
    /// a type, and a wrong guess either fails confusingly or -- worse -- succeeds against a file
    /// whose bytes happen to be readable as the guessed type.
    /// </summary>
    internal static string? RootReaderName(string rootDirectory, string assetName)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(assetName);

        string path = XnaContentPath.ToFilePath(rootDirectory, assetName, ".xnb");
        if (!File.Exists(path))
        {
            throw new ContentLoadException($"Content file '{path}' was not found.");
        }

        return Read(path, assetName, static reader => reader.PeekRootReaderName());
    }
}
