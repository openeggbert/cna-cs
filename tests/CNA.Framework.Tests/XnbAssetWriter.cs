using System.Text;
using CNA.Content.Xnb;

namespace CNA.Tests;

/// <summary>
/// Writes the body of a <c>.xnb</c> object graph -- the type-reader table, the shared-resource
/// count and the root object -- so a reader can be exercised against bytes laid out the way the
/// format specifies rather than the way the reader happens to consume them.
///
/// <b>Where this is and is not the right tool.</b> It is right for readers whose byte format is
/// transcribed from a decompiled XNA reader and whose failure mode is a stream that desynchronises:
/// the writer is written from the *format*, the reader from the *reader*, and a disagreement shows
/// up as a wrong value rather than as both sides sharing one mistake. It is not a substitute for
/// the real MonoGame-compiled fixture <see cref="XnbModelReaderTests"/> uses, and no test here
/// should be read as evidence about what a content pipeline actually emits.
///
/// The header is deliberately absent: <see cref="XnbContentReader.Create(BinaryReader, string)"/> begins immediately
/// after it, so writing one would only test <see cref="XnbHeader"/> a second time.
/// </summary>
internal static class XnbAssetWriter
{
    /// <summary>Builds the body. <paramref name="typeReaders"/> are written in table order, so a
    /// 1-based index <paramref name="writeRoot"/> writes selects the entry at that position.</summary>
    internal static byte[] Build(
        IReadOnlyList<string> typeReaders,
        Action<BinaryWriter> writeRoot,
        int sharedResourceCount = 0,
        Action<BinaryWriter>? writeSharedResources = null)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write7BitEncodedInt(typeReaders.Count);
            foreach (string reader in typeReaders)
            {
                writer.Write(reader);
                writer.Write(0);
            }

            writer.Write7BitEncodedInt(sharedResourceCount);
            writeRoot(writer);
            writeSharedResources?.Invoke(writer);
        }

        return stream.ToArray();
    }

    /// <summary>Reads a body built by <see cref="Build"/> through the real reader.</summary>
    internal static object? ReadRoot(byte[] body)
    {
        using var reader = new BinaryReader(new MemoryStream(body), Encoding.UTF8);
        return XnbContentReader.Create(reader).ReadRootObjectAndResolveSharedResources();
    }

    /// <summary>The same, for an asset whose readers resolve external references relative to
    /// <paramref name="assetName"/>.</summary>
    internal static object? ReadRoot(byte[] body, string assetName)
    {
        using var reader = new BinaryReader(new MemoryStream(body), Encoding.UTF8);
        return XnbContentReader.Create(reader, assetName).ReadRootObjectAndResolveSharedResources();
    }
}
