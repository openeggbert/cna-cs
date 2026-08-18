namespace CNA.Content.Xnb;

/// <summary>
/// Drives the real <c>.xnb</c> "object graph" reading protocol, confirmed byte-for-byte against
/// the real openeggbert/cna C++ engine's own <c>ContentReader</c>/<c>XnbTypeReaderTable</c> and a
/// real, uncompressed, MonoGame-compiled <c>Model</c> asset (see <c>NEXT.md</c> for the research
/// this was built from):
///
/// 1. Right after the header (<see cref="XnbHeader"/>), a **type-reader table**: a count, then
///    that many (name, version) pairs -- <see cref="Create"/> reads this once, up front.
/// 2. A **shared-resource count**, immediately after the table.
/// 3. The **root object**, read via the dispatch protocol every object in this file uses: a 7-bit
///    type-reader index (0 = null; otherwise a 1-based index into the table from step 1) followed
///    by that reader's own format.
/// 4. Exactly <c>sharedResourceCount</c> more objects, read the same way, in file order -- these
///    are the actual bytes of every <see cref="ReadSharedResource"/> reference encountered while
///    reading the root object. A shared-resource *reference* (step 3's nested reads) only reads a
///    7-bit index at the point it's encountered and defers everything else: the referenced
///    object's real bytes are NOT inline there, they come later, in this step. This two-pass "read
///    every object, then run every registered fixup" order is load-bearing -- a mesh part's
///    <c>VertexBuffer</c> reference is read (as an index only) long before that buffer's actual
///    bytes appear in the file, and two mesh parts can share one buffer by referencing the same
///    index.
///
/// This project reads only real, non-string type-reader names it explicitly registers a reader
/// for (see <see cref="Readers"/>) -- an asset referencing an unregistered type-reader (a real,
/// valid <c>.xnb</c> feature -- e.g. a custom `ContentTypeReader` subclass, or a built-in one this
/// project hasn't ported yet) fails with a clear <see cref="ContentLoadException"/> naming the
/// missing reader, rather than silently misreading the rest of the file.
/// </summary>
internal sealed class XnbContentReader
{
    private static readonly Dictionary<string, Func<XnbContentReader, object?>> Readers = new()
    {
        ["Microsoft.Xna.Framework.Content.StringReader"] = r => r._reader.ReadString(),
        ["Microsoft.Xna.Framework.Content.ModelReader"] = XnbModelReader.Read,
        ["Microsoft.Xna.Framework.Content.VertexBufferReader"] = XnbVertexBufferReader.Read,
        ["Microsoft.Xna.Framework.Content.IndexBufferReader"] = XnbIndexBufferReader.Read,
        ["Microsoft.Xna.Framework.Content.BasicEffectReader"] = XnbBasicEffectReader.Read,
        ["Microsoft.Xna.Framework.Content.Texture2DReader"] = XnbTexture2DReader.Read,
        ["Microsoft.Xna.Framework.Content.SpriteFontReader"] = XnbSpriteFontReader.Read,

        // The generic readers a SpriteFont needs. Keyed by the full generic name including the
        // element type, because that is what the file's own type-reader table spells and because
        // the element type is what decides how each entry is read -- there is no runtime generic
        // instantiation happening here, just four concrete formats.
        ["Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle]]"] =
            r => r.ReadInlineList(static x => x.ReadRectangle()),
        ["Microsoft.Xna.Framework.Content.ListReader`1[[System.Char]]"] =
            r => r.ReadInlineList(static x => x.ReadChar()),
        ["Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3]]"] =
            r => r.ReadInlineList(static x => x.ReadVector3()),
        // A NullableReader with no value answers null, which ReadObject already uses for "the
        // stream said type index 0". Collapsing the two is right here rather than sloppy: for a
        // Nullable<char>, "the nullable is empty" and "there was no object" are the same answer to
        // the only question a caller asks.
        ["Microsoft.Xna.Framework.Content.NullableReader`1[[System.Char]]"] =
            r => r.ReadBoolean() ? r.ReadChar() : null,
    };

    private readonly BinaryReader _reader;
    private readonly List<string> _typeReaderNames;
    private readonly List<List<Action<object>>> _sharedResourceFixups;

    private XnbContentReader(BinaryReader reader, List<string> typeReaderNames, int sharedResourceCount)
    {
        _reader = reader;
        _typeReaderNames = typeReaderNames;
        _sharedResourceFixups = new List<List<Action<object>>>(sharedResourceCount);
        for (int i = 0; i < sharedResourceCount; i++)
        {
            _sharedResourceFixups.Add([]);
        }
    }

    /// <summary>Reads the type-reader table and shared-resource count -- call once, immediately
    /// after <see cref="XnbHeader.Read"/>, before reading the root object.</summary>
    internal static XnbContentReader Create(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        int typeReaderCount = reader.Read7BitEncodedInt();
        if (typeReaderCount is < 0 or > 4096)
        {
            throw new ContentLoadException($"Corrupt .xnb file: implausible type reader count {typeReaderCount}.");
        }

        var typeReaderNames = new List<string>(typeReaderCount);
        for (int i = 0; i < typeReaderCount; i++)
        {
            // Real .xnb entries are assembly-qualified ("Foo.BarReader, SomeAssembly,
            // Version=...,..."); this project has no assembly/version concept to check, so only
            // the canonical reader name (before the first comma) is kept -- sufficient for every
            // reader Model needs (none of them are open generic types like ListReader`1[[...]],
            // which is the one case a fuller name parse would actually matter for).
            string rawName = reader.ReadString();
            _ = reader.ReadInt32(); // reader version -- always 0 for every built-in reader in practice
            typeReaderNames.Add(NormalizeTypeReaderName(rawName));
        }

        int sharedResourceCount = reader.Read7BitEncodedInt();
        if (sharedResourceCount < 0)
        {
            throw new ContentLoadException($"Corrupt .xnb file: negative shared resource count {sharedResourceCount}.");
        }

        return new XnbContentReader(reader, typeReaderNames, sharedResourceCount);
    }

    /// <summary>Reads the root object, then every shared-resource object in file order, then runs
    /// every fixup registered against each -- see this type's own doc comment for why this order
    /// is load-bearing.</summary>
    internal object? ReadRootObjectAndResolveSharedResources()
    {
        object? root = ReadObject();

        foreach (List<Action<object>> fixups in _sharedResourceFixups)
        {
            object? resource = ReadObject();
            if (resource is null)
            {
                continue;
            }

            foreach (Action<object> fixup in fixups)
            {
                fixup(resource);
            }
        }

        return root;
    }

    /// <summary>Reads one object via the dispatch protocol described in this type's own doc
    /// comment: a 7-bit type-reader index (0 = null), then that reader's own format.</summary>
    internal object? ReadObject()
    {
        int index = _reader.Read7BitEncodedInt();
        if (index == 0)
        {
            return null;
        }

        if (index < 1 || index > _typeReaderNames.Count)
        {
            throw new ContentLoadException($"Corrupt .xnb file: type reader index {index} out of range.");
        }

        string name = _typeReaderNames[index - 1];
        if (!Readers.TryGetValue(name, out Func<XnbContentReader, object?>? read))
        {
            throw new ContentLoadException($"This .xnb file uses content type reader '{name}', which this project's .xnb reader does not (yet) support.");
        }

        return read(this);
    }

    internal T ReadObject<T>() where T : class => RequireType<T>(ReadObject(), "an object read from the stream");

    /// <summary>Shared cast-and-throw helper: one place defining what "wrong type" means for a
    /// <c>.xnb</c> value, used both by <see cref="ReadObject{T}"/> (a freshly-read object) and by
    /// <c>XnbModelReader</c>'s shared-resource fixups (an already-resolved object handed back
    /// later, via <see cref="ReadSharedResource"/>'s two-pass mechanism) -- a code-review finding:
    /// these two call sites previously had their own separate, near-identical cast-and-throw
    /// copies, one of them not null-safe, with no shared definition to keep them in sync.</summary>
    internal static T RequireType<T>(object? value, string what) where T : class
    {
        if (value is not T typed)
        {
            throw new ContentLoadException(
                $"Corrupt .xnb file: expected {what} to be {typeof(T).Name}, but it was {value?.GetType().Name ?? "null"}.");
        }

        return typed;
    }

    /// <summary>Reads a shared-resource *reference*: a 7-bit index (0 = no reference). A nonzero
    /// index registers <paramref name="fixup"/> to run once that resource's real bytes are read,
    /// later in the stream -- see this type's own doc comment. Does not read the resource's bytes
    /// now.</summary>
    internal void ReadSharedResource(Action<object> fixup)
    {
        ArgumentNullException.ThrowIfNull(fixup);

        int index = _reader.Read7BitEncodedInt();
        if (index == 0)
        {
            return;
        }

        if (index < 1 || index > _sharedResourceFixups.Count)
        {
            throw new ContentLoadException($"Corrupt .xnb file: shared resource index {index} out of range.");
        }

        _sharedResourceFixups[index - 1].Add(fixup);
    }

    /// <summary>
    /// Reduces an assembly-qualified <c>.xnb</c> type-reader name to the canonical name this
    /// project keys its reader table by.
    ///
    /// Trimming at the first comma is what this used to do, and it is wrong for a generic reader:
    /// <c>ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=...]]</c>
    /// would be cut down to <c>ListReader`1[[Microsoft.Xna.Framework.Rectangle</c>. That never
    /// mattered while <c>Model</c> was the only asset read here (none of its readers are generic),
    /// and the old comment said so -- <c>SpriteFont</c> is the case it warned about. Assembly
    /// qualification is stripped at every bracket depth instead, so both the outer reader name and
    /// each element type keep their identity and lose their assembly.
    /// </summary>
    internal static string NormalizeTypeReaderName(string rawName)
    {
        ArgumentNullException.ThrowIfNull(rawName);

        var result = new System.Text.StringBuilder(rawName.Length);
        int depth = 0;
        bool skipping = false;

        foreach (char c in rawName)
        {
            if (c == '[')
            {
                depth++;
                skipping = false;
                result.Append(c);
                continue;
            }

            if (c == ']')
            {
                depth--;
                skipping = false;
                result.Append(c);
                continue;
            }

            if (c == ',')
            {
                // Everything from here to the end of this bracket level is assembly qualification.
                // At depth zero that means the rest of the string.
                if (depth == 0)
                {
                    break;
                }

                skipping = true;
                continue;
            }

            if (!skipping)
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>Reads a <c>ListReader</c> body: a 32-bit count followed by that many inline
    /// elements. Inline, not object-dispatched -- XNA writes a value-type list's elements directly,
    /// with no per-element type-reader index.</summary>
    private List<T> ReadInlineList<T>(Func<XnbContentReader, T> readElement)
    {
        int count = _reader.ReadInt32();
        if (count is < 0 or > 1_000_000)
        {
            throw new ContentLoadException($"Corrupt .xnb file: implausible list length {count}.");
        }

        var items = new List<T>(Math.Min(count, 1024));
        for (int i = 0; i < count; i++)
        {
            items.Add(readElement(this));
        }

        return items;
    }

    /// <summary>Reads a list written by one of the registered <c>ListReader</c> entries, with a
    /// message naming what was being read when the type does not match.</summary>
    internal IReadOnlyList<T> ReadList<T>(string what)
    {
        object? value = ReadObject();
        if (value is List<T> list)
        {
            return list;
        }

        throw new ContentLoadException(
            $"Corrupt .xnb file: expected a list of {typeof(T).Name} for {what}, got {value?.GetType().Name ?? "null"}.");
    }

    internal string ReadString() => _reader.ReadString();

    /// <summary>
    /// Reads one <c>char</c> the way the format writes it: UTF-8 encoded and therefore *variable
    /// width*, one to three bytes, not a fixed 16-bit code unit.
    ///
    /// <see cref="BinaryReader.ReadChar"/> is what does the decoding, because XNA's own
    /// <c>ContentReader</c> is a <see cref="BinaryReader"/> over UTF-8 and its <c>ReadChar</c> is
    /// this one. Reading a fixed <see cref="ushort"/> instead parses a font's ASCII character map
    /// at double width and desynchronises everything after it -- which is exactly what happened,
    /// and what the fixture-backed tests caught.
    /// </summary>
    internal char ReadChar() => _reader.ReadChar();

    internal Rectangle ReadRectangle() =>
        new(_reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32());

    internal int ReadInt32() => _reader.ReadInt32();

    internal uint ReadUInt32() => _reader.ReadUInt32();

    internal float ReadSingle() => _reader.ReadSingle();

    internal bool ReadBoolean() => _reader.ReadBoolean();

    /// <summary>Like <see cref="BinaryReader.ReadBytes(int)"/>, but throws on a short read instead
    /// of silently returning fewer bytes than asked for -- the BCL method truncates on a
    /// short/truncated stream rather than throwing, which would otherwise let a corrupt file
    /// silently produce an undersized buffer instead of a clear error.</summary>
    internal byte[] ReadExactBytes(int count)
    {
        byte[] data = _reader.ReadBytes(count);
        if (data.Length != count)
        {
            throw new ContentLoadException($"Corrupt or truncated .xnb file: expected {count} bytes, got {data.Length}.");
        }

        return data;
    }

    internal Vector3 ReadVector3() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    /// <summary>Raw inline field-order read (<c>M11,M12,M13,M14,M21,...,M44</c>), matching the real
    /// C++ engine's own <c>ContentReader::ReadMatrix()</c> -- **not** wrapped in the object-graph
    /// dispatch protocol (no leading type-reader index).</summary>
    internal Matrix ReadMatrix() => new(
        _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(),
        _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(),
        _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(),
        _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

    internal BoundingSphere ReadBoundingSphere() => new(ReadVector3(), _reader.ReadSingle());

    /// <summary>A "bone reference": 1 byte if <paramref name="boneCount"/> is less than 255, else a
    /// full <c>uint32</c> -- real XNA's own size-optimizing encoding for <c>ModelReader</c>, ported
    /// exactly, including the <c>&lt; 255</c> threshold (not <c>&lt;= 255</c> or <c>&lt; 256</c>).
    /// A raw value of <c>0</c> means "no bone" (returned here as <c>-1</c>); a real reference is
    /// stored 1-based, so a nonzero raw value is decremented to the real, 0-based bone index.
    /// Bounds-checked against <paramref name="boneCount"/> here -- a code-review finding: every
    /// caller (<see cref="XnbModelReader"/>'s bone-hierarchy/mesh/root reads, and
    /// <see cref="XnbModelBuilder"/>'s own indexing into its bones list) trusted this value without
    /// re-checking it, so a corrupt file with an out-of-range reference would otherwise surface as
    /// an unhandled <see cref="ArgumentOutOfRangeException"/> deep in list-indexing code instead of
    /// the clear <see cref="ContentLoadException"/> every other corrupt-input case in this feature
    /// produces.</summary>
    internal int ReadBoneReference(int boneCount)
    {
        uint raw = boneCount < 255 ? _reader.ReadByte() : _reader.ReadUInt32();
        if (raw == 0)
        {
            return -1;
        }

        if (raw > boneCount)
        {
            throw new ContentLoadException($"Corrupt .xnb file: bone reference {raw} is out of range for {boneCount} bones.");
        }

        return (int)(raw - 1);
    }

    /// <summary>Reads and rejects an object's <c>Tag</c> -- matching real XNA's own <c>ModelReader</c>
    /// exactly: <c>Tag</c> is read via the ordinary object-graph dispatch protocol purely to keep
    /// the stream position correct for whatever follows, but real content pipeline output never
    /// actually sets a non-null <c>Tag</c> on <see cref="Graphics.Model"/>/<see cref="Graphics.ModelMesh"/>/
    /// <see cref="Graphics.ModelMeshPart"/> -- a non-null value here means either a <c>Tag</c> this
    /// reader doesn't know how to represent, or a genuinely corrupt file, so it's rejected with a
    /// clear exception (a real, documented scope line, not a silently-dropped value).</summary>
    internal void RejectNonNullTag(string context)
    {
        object? tag = ReadObject();
        if (tag is not null)
        {
            throw new ContentLoadException($"{context}: this .xnb file sets a non-null Tag, which this project's .xnb reader does not support.");
        }
    }
}
