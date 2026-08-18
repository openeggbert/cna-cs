namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible <c>ContentReader</c>. A thin re-typing wrapper rather than a
/// subclass, because <see cref="CNA.Content.ContentReader"/>'s only constructor is internal
/// (readers are created by the content system, not by game code). The primitive reads come from
/// the wrapped reader's inherited <see cref="System.IO.BinaryReader"/> surface, exposed here so
/// XNA source that calls them keeps working.</summary>
public class ContentReader
{
    private readonly CNA.Content.ContentReader _reader;

    internal ContentReader(CNA.Content.ContentReader reader)
    {
        _reader = reader;
    }

    /// <summary>The wrapped reader, for the compat <see cref="ContentTypeReader"/> to pass back
    /// into the CNA layer.</summary>
    internal CNA.Content.ContentReader Framework => _reader;

    public ContentManager ContentManager => (ContentManager)_reader.ContentManager;

    public string AssetName => _reader.AssetName;

    public int Version => _reader.Version;

    public byte Platform => _reader.Platform;

    public bool ReadBoolean() => _reader.ReadBoolean();

    public byte ReadByte() => _reader.ReadByte();

    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

    public char ReadChar() => _reader.ReadChar();

    public double ReadDouble() => _reader.ReadDouble();

    public short ReadInt16() => _reader.ReadInt16();

    public int ReadInt32() => _reader.ReadInt32();

    public long ReadInt64() => _reader.ReadInt64();

    public sbyte ReadSByte() => _reader.ReadSByte();

    public float ReadSingle() => _reader.ReadSingle();

    public string ReadString() => _reader.ReadString();

    public ushort ReadUInt16() => _reader.ReadUInt16();

    public uint ReadUInt32() => _reader.ReadUInt32();

    public ulong ReadUInt64() => _reader.ReadUInt64();

    public Matrix ReadMatrix() => _reader.ReadMatrix();

    public Quaternion ReadQuaternion() => _reader.ReadQuaternion();

    public Vector2 ReadVector2() => _reader.ReadVector2();

    public Vector3 ReadVector3() => _reader.ReadVector3();

    public Vector4 ReadVector4() => _reader.ReadVector4();

    public Color ReadColor() => _reader.ReadColor();

    public bool ReadObjectTag() => _reader.ReadObjectTag();

    public byte[] ReadBytesExact(int count, string readerName) => _reader.ReadBytesExact(count, readerName);
}
