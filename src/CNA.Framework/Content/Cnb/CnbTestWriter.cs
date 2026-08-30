using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// Writes a minimal <c>.cnb</c> container through CNA's own encoder.
///
/// <b>Why this exists, and why it is internal.</b> A read path needs something to read, and the
/// honest fixture for "can this parse CNA's container" is one CNA itself wrote -- not a byte array
/// assembled here from a reading of the format, which would test this repository's understanding
/// against itself, and not somebody's content copied into the tree.
///
/// It is deliberately not the public writer. <c>cnb.h</c>'s writer family is thirteen routes with
/// compression, external references, embedded textures and schema validation, and projecting that
/// is a separate piece of work from reading a container. This is the smallest thing that can
/// produce a valid file.
/// </summary>
internal sealed class CnbTestWriter : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public CnbTestWriter(uint assetTypeId, uint assetSchemaVersion)
    {
        CnaResult result = Native.cna_cnb_writer_create(assetTypeId, assetSchemaVersion, out CnaHandle writer);
        CnaException.ThrowIfFailed(result, nameof(CnbTestWriter));
        _handle = new NativeResourceHandle(
            writer.AsNint,
            h => Native.cna_cnb_writer_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>Appends one chunk. <paramref name="type"/> is the four-character identifier packed
    /// the way CNA packs it, least significant byte first.</summary>
    public unsafe void AddChunk(uint type, ReadOnlySpan<byte> data, uint alignment = 4)
    {
        fixed (byte* bytes = data)
        {
            CnaResult result = Native.cna_cnb_writer_add_chunk(
                Handle, type, bytes, (ulong)data.Length, 0u, alignment);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(AddChunk));
        }
    }

    public void WriteToFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        CnaResult result = CnaStringMarshal.WithStringView(
            path, view => Native.cna_cnb_writer_write_to_file(Handle, view));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(WriteToFile));
    }

    /// <summary>Packs four characters the way CNA's chunk identifiers are packed.</summary>
    public static uint ChunkId(string fourCharacters)
    {
        ArgumentNullException.ThrowIfNull(fourCharacters);
        if (fourCharacters.Length != 4)
        {
            throw new ArgumentException("A CNB chunk identifier is exactly four characters.", nameof(fourCharacters));
        }

        return (uint)fourCharacters[0]
            | ((uint)fourCharacters[1] << 8)
            | ((uint)fourCharacters[2] << 16)
            | ((uint)fourCharacters[3] << 24);
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    /// <summary>
    /// Writes a complete single-representation RGBA8 2D texture container, through CNA's own
    /// texture encoder rather than through this writer's chunk API.
    ///
    /// The encoder is a different route from <c>writer_add_chunk</c> and is the right one here for
    /// the same reason this whole type exists: a texture container is a set of chunks with a
    /// defined relationship, and assembling those by hand would encode this repository's reading of
    /// the schema into the fixture. <c>cna_cnb_encode_texture2d</c> is CNA stating it.
    ///
    /// RGBA8 is the only format CNA's encoder writes; decoding accepts every format in
    /// <see cref="CnbTextureFormat"/>. So a fixture cannot exercise the representation choice
    /// across formats, and a test that needs to must say so rather than pretend otherwise.
    /// </summary>
    public static unsafe void WriteRgba8Texture2D(string path, int width, int height, ReadOnlySpan<byte> rgba, string contentName = "")
    {
        ArgumentNullException.ThrowIfNull(path);

        CnaHandle texture;
        fixed (byte* pixels = rgba)
        {
            CnaResult created = Native.cna_cnb_texture_data_create_rgba8(
                (uint)width, (uint)height, pixels, (ulong)rgba.Length, out texture);
            CnaException.ThrowIfFailed(created, nameof(WriteRgba8Texture2D));
        }

        try
        {
            byte[]? encoded = null;
            CnaResult encodeResult = CnaStringMarshal.WithStringView(contentName, view =>
            {
                // BufferTooSmall is the expected answer to a zero-capacity size query, not a
                // failure: the route writes the required count and performs no partial write.
                CnaResult sizeResult = Native.cna_cnb_encode_texture2d(texture, view, null, 0, out ulong required);
                if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
                {
                    return sizeResult;
                }

                var bytes = new byte[checked((int)required)];
                fixed (byte* destination = bytes)
                {
                    CnaResult result = Native.cna_cnb_encode_texture2d(
                        texture, view, destination, (ulong)bytes.Length, out ulong written);
                    if (!result.IsSuccess())
                    {
                        return result;
                    }

                    if (written != (ulong)bytes.Length)
                    {
                        throw new CnaException(
                            $"The CNB texture encoder asked for {required} bytes and wrote {written}.");
                    }
                }

                encoded = bytes;
                return CnaResult.Success;
            });

            CnaException.ThrowIfFailed(encodeResult, nameof(WriteRgba8Texture2D));
            File.WriteAllBytes(path, encoded!);
        }
        finally
        {
            _ = Native.cna_cnb_texture_data_destroy(texture);
        }
    }
}
