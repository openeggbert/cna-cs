using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// One chunk of a <see cref="CnbDocument"/>'s table of contents.
///
/// A value type because it is a snapshot of the entry, not a live view into the document: reading
/// it after the document is disposed answers what the file said rather than touching freed memory.
/// </summary>
public readonly struct CnbChunk
{
    internal CnbChunk(CnaCnbChunkEntry entry)
    {
        Type = entry.Type;
        Offset = entry.Offset;
        StoredSize = entry.StoredSize;
        UncompressedSize = entry.UncompressedSize;
        Alignment = entry.Alignment;
        Checksum = entry.Checksum;
    }

    /// <summary>The chunk's four-character identifier, as CNA stores it.</summary>
    public uint Type { get; }

    /// <summary>Its four characters, in the order they appear in the file.</summary>
    public string TypeName =>
        new([(char)(Type & 0xFF), (char)((Type >> 8) & 0xFF), (char)((Type >> 16) & 0xFF), (char)((Type >> 24) & 0xFF)]);

    public ulong Offset { get; }

    public ulong StoredSize { get; }

    /// <summary>What the chunk expands to once decompressed; equal to <see cref="StoredSize"/> for
    /// an uncompressed chunk.</summary>
    public ulong UncompressedSize { get; }

    public uint Alignment { get; }

    public uint Checksum { get; }
}

/// <summary>
/// A parsed <c>.cnb</c> container: CNA's own binary content format.
///
/// <b>Not XNA, and deliberately not in <c>Microsoft.Xna.Framework</c>.</b> XNA has one content
/// container and this is a second one, so exposing it through <c>ContentManager.Load&lt;T&gt;</c>
/// would change a contract that is checked member for member against XNA's own metadata. It lives
/// here, in CNA's own vocabulary, and a game opts into it explicitly.
///
/// <b>This is the read path only.</b> <c>cnb.h</c> has 272 routes -- encoders, model builders,
/// sprite tooling -- and projecting all of them would be a worse API than none. What a game needs
/// first is to open a container, find out what it holds and get at its bytes; that is what this is.
///
/// <b>Ownership.</b> The document handle is owned: this object created it and destroys it. A
/// <see cref="CnbChunk"/> is a copied snapshot rather than a pointer into the document, so no chunk
/// can outlive the memory it describes. Chunk *data* is copied into a caller array for the same
/// reason -- CNA's own accessor writes into a caller buffer, which is the shape that makes a
/// dangling view impossible rather than merely unlikely.
/// </summary>
public sealed class CnbDocument : IDisposable
{
    private readonly NativeResourceHandle _handle;

    private CnbDocument(nint handleValue)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_document_destroy(new CnaHandle(h)).IsSuccess());
    }

    private CnbDocument(nint handleValue, bool owned)
    {
        _handle = owned
            ? new NativeResourceHandle(
                handleValue, h => Native.cna_cnb_document_destroy(new CnaHandle(h)).IsSuccess())
            : new NativeResourceHandle(handleValue, static _ => true);
    }

    /// <summary>
    /// A non-owning view of a document CNA owns, for the duration of a callback.
    ///
    /// <b>Non-owning is the whole point.</b> A <see cref="CnbLoaderRegistration"/> callback is handed
    /// the container borrowed, and a wrapper that destroyed it on the way out would take down the
    /// caller's document mid-load. The release delegate is a no-op rather than absent so that
    /// <c>using</c> reads normally at the call site and does the right thing.
    /// </summary>
    internal static CnbDocument Borrowing(CnaHandle document) => new(document.AsNint, owned: false);

    /// <summary>
    /// Parses a <c>.cnb</c> file with CNA's own default reader ceilings.
    ///
    /// The limits come from <c>cna_cnb_read_limits_init</c> rather than being constructed here: they
    /// bound how much memory a malformed file can make a reader allocate, and choosing them is
    /// CNA's business. A zeroed structure is not the default -- it is a reader that will accept
    /// nothing.
    /// </summary>
    public static CnbDocument Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var limits = CnaCnbReadLimits.Versioned();
        CnaResult limitsResult = Native.cna_cnb_read_limits_init(ref limits);
        CnaException.ThrowIfFailed(limitsResult, nameof(Open));

        CnaHandle document = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            path, view => Native.cna_cnb_document_parse_file(view, in limits, out document));
        CnaException.ThrowIfFailed(result, nameof(Open));

        return new CnbDocument(document.AsNint);
    }

    /// <summary>The container format's major version, which is the file's own, not CNA's.</summary>
    public int ContainerMajorVersion => ReadUInt16(Native.cna_cnb_document_get_container_major);

    public int ContainerMinorVersion => ReadUInt16(Native.cna_cnb_document_get_container_minor);

    /// <summary>Which kind of asset the container holds, as CNA's own identity.</summary>
    public uint AssetTypeId => ReadUInt32(Native.cna_cnb_document_get_asset_type_id);

    /// <summary>The asset schema's version, which a loader uses to decide whether it understands
    /// the payload.</summary>
    public uint AssetSchemaVersion => ReadUInt32(Native.cna_cnb_document_get_asset_schema_version);

    public int ChunkCount
    {
        get
        {
            CnaResult result = Native.cna_cnb_document_get_chunk_count(Handle, out ulong count);
            CnaException.ThrowIfFailed(result, nameof(ChunkCount));
            GC.KeepAlive(this);
            return checked((int)count);
        }
    }

    /// <summary>One entry of the table of contents.</summary>
    public CnbChunk GetChunk(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var entry = CnaCnbChunkEntry.Versioned();
        CnaResult result = Native.cna_cnb_document_get_chunk(Handle, (ulong)index, ref entry);
        CnaException.ThrowIfFailed(result, nameof(GetChunk));
        GC.KeepAlive(this);
        return new CnbChunk(entry);
    }

    /// <summary>
    /// A chunk's bytes, decompressed, in a fresh array.
    ///
    /// Copied rather than exposed as a span over native memory: the document owns those bytes, and a
    /// span would stay valid-looking after <see cref="Dispose"/>. The size comes from the entry's
    /// own <see cref="CnbChunk.UncompressedSize"/>, so the array is exactly the payload.
    /// </summary>
    public unsafe byte[] ReadChunkData(int index)
    {
        CnbChunk chunk = GetChunk(index);
        var data = new byte[checked((int)chunk.UncompressedSize)];
        if (data.Length == 0)
        {
            return data;
        }

        fixed (byte* destination = data)
        {
            CnaResult result = Native.cna_cnb_document_copy_chunk_data(
                Handle, (ulong)index, destination, (ulong)data.Length, out ulong written);
            CnaException.ThrowIfFailed(result, nameof(ReadChunkData));
            GC.KeepAlive(this);

            if (written != (ulong)data.Length)
            {
                throw new CnaException(
                    $"CNB chunk {index} reported {chunk.UncompressedSize} bytes but produced {written}.");
            }
        }

        return data;
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    /// <summary>The document handle, for a decoder in this namespace that needs to read it.
    /// Borrowed: the document remains its only owner, and the caller must keep this object alive
    /// across the call.</summary>
    internal CnaHandle NativeHandle => Handle;

    private delegate CnaResult UInt16Accessor(CnaHandle document, out ushort value);

    private delegate CnaResult UInt32Accessor(CnaHandle document, out uint value);

    private int ReadUInt16(UInt16Accessor accessor)
    {
        CnaResult result = accessor(Handle, out ushort value);
        CnaException.ThrowIfFailed(result, nameof(CnbDocument));
        GC.KeepAlive(this);
        return value;
    }

    private uint ReadUInt32(UInt32Accessor accessor)
    {
        CnaResult result = accessor(Handle, out uint value);
        CnaException.ThrowIfFailed(result, nameof(CnbDocument));
        GC.KeepAlive(this);
        return value;
    }
}
