namespace Microsoft.Xna.Framework.Content;

/// <summary>Builds and owns the type-reader table for one XNB asset.</summary>
public sealed class ContentTypeReaderManager
{
    private readonly Dictionary<Type, ContentTypeReader> _readersByTargetType = [];

    internal ContentTypeReaderManager()
    {
    }

    /// <summary>Returns the reader for a target type in this asset, if it has one.</summary>
    public ContentTypeReader? GetTypeReader(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        return _readersByTargetType.TryGetValue(targetType, out ContentTypeReader? reader) ? reader : null;
    }

    internal ContentTypeReader[] LoadAssetReaders(ContentReader input, out int[] versions)
    {
        ArgumentNullException.ThrowIfNull(input);

        int count = input.Read7BitEncodedInt32();
        if (count is < 0 or > 4096)
        {
            throw new ContentLoadException($"Content asset '{input.AssetName}' has invalid type reader count {count}.");
        }

        var readers = new ContentTypeReader[count];
        versions = new int[count];
        for (int index = 0; index < count; index++)
        {
            string serializedName = input.ReadString();
            readers[index] = CreateReader(serializedName, input.AssetName);
            versions[index] = input.ReadInt32();

            // A single target type can legitimately appear under more than one reader in an XNB,
            // but ReadRawObject<T>() follows XNA's first-table-entry lookup.
            _readersByTargetType.TryAdd(readers[index].TargetType, readers[index]);
        }

        foreach (ContentTypeReader reader in readers)
        {
            reader.Initialize(this);
        }

        return readers;
    }

    private static ContentTypeReader CreateReader(string serializedName, string assetName)
    {
        if (string.IsNullOrWhiteSpace(serializedName))
        {
            throw new ContentLoadException($"Content asset '{assetName}' declares an empty content type reader name.");
        }

        if (BuiltinReaders.TryCreate(serializedName, out ContentTypeReader? builtIn) && builtIn is not null)
        {
            return builtIn;
        }

        Type? readerType = Type.GetType(serializedName, throwOnError: false);
        if (readerType is null || !typeof(ContentTypeReader).IsAssignableFrom(readerType))
        {
            throw new ContentLoadException(
                $"Could not find ContentTypeReader Type '{serializedName}' while loading '{assetName}'. " +
                "Ensure the reader assembly name in the XNB matches the loaded assembly.");
        }

        try
        {
            return Activator.CreateInstance(readerType, nonPublic: true) as ContentTypeReader
                ?? throw new ContentLoadException(
                    $"Content type reader '{serializedName}' does not have a usable parameterless constructor.");
        }
        catch (ContentLoadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ContentLoadException(
                $"Failed to construct content type reader '{serializedName}' while loading '{assetName}'.", exception);
        }
    }
}

/// <summary>Reader factories for primitive XNA reader names commonly nested in custom assets.</summary>
internal static class BuiltinReaders
{
    internal static bool TryCreate(string serializedName, out ContentTypeReader? reader)
    {
        string name = StripAssemblyQualification(serializedName);
        reader = name switch
        {
            "Microsoft.Xna.Framework.Content.BooleanReader" => new BooleanReader(),
            "Microsoft.Xna.Framework.Content.ByteReader" => new ByteReader(),
            "Microsoft.Xna.Framework.Content.CharReader" => new CharReader(),
            "Microsoft.Xna.Framework.Content.DoubleReader" => new DoubleReader(),
            "Microsoft.Xna.Framework.Content.Int16Reader" => new Int16Reader(),
            "Microsoft.Xna.Framework.Content.Int32Reader" => new Int32Reader(),
            "Microsoft.Xna.Framework.Content.Int64Reader" => new Int64Reader(),
            "Microsoft.Xna.Framework.Content.SByteReader" => new SByteReader(),
            "Microsoft.Xna.Framework.Content.SingleReader" => new SingleReader(),
            "Microsoft.Xna.Framework.Content.StringReader" => new StringReader(),
            "Microsoft.Xna.Framework.Content.UInt16Reader" => new UInt16Reader(),
            "Microsoft.Xna.Framework.Content.UInt32Reader" => new UInt32Reader(),
            "Microsoft.Xna.Framework.Content.UInt64Reader" => new UInt64Reader(),
            "Microsoft.Xna.Framework.Content.Vector2Reader" => new Vector2Reader(),
            "Microsoft.Xna.Framework.Content.Vector3Reader" => new Vector3Reader(),
            "Microsoft.Xna.Framework.Content.Vector4Reader" => new Vector4Reader(),
            "Microsoft.Xna.Framework.Content.MatrixReader" => new MatrixReader(),
            "Microsoft.Xna.Framework.Content.QuaternionReader" => new QuaternionReader(),
            "Microsoft.Xna.Framework.Content.ColorReader" => new ColorReader(),
            _ => null,
        };
        return reader is not null;
    }

    private static string StripAssemblyQualification(string name)
    {
        int depth = 0;
        for (int index = 0; index < name.Length; index++)
        {
            switch (name[index])
            {
                case '[': depth++; break;
                case ']': depth--; break;
                case ',' when depth == 0: return name[..index];
            }
        }

        return name;
    }

    private sealed class BooleanReader : ContentTypeReader<bool> { protected internal override bool Read(ContentReader input, bool existingInstance) => input.ReadBoolean(); }
    private sealed class ByteReader : ContentTypeReader<byte> { protected internal override byte Read(ContentReader input, byte existingInstance) => input.ReadByte(); }
    private sealed class CharReader : ContentTypeReader<char> { protected internal override char Read(ContentReader input, char existingInstance) => input.ReadChar(); }
    private sealed class DoubleReader : ContentTypeReader<double> { protected internal override double Read(ContentReader input, double existingInstance) => input.ReadDouble(); }
    private sealed class Int16Reader : ContentTypeReader<short> { protected internal override short Read(ContentReader input, short existingInstance) => input.ReadInt16(); }
    private sealed class Int32Reader : ContentTypeReader<int> { protected internal override int Read(ContentReader input, int existingInstance) => input.ReadInt32(); }
    private sealed class Int64Reader : ContentTypeReader<long> { protected internal override long Read(ContentReader input, long existingInstance) => input.ReadInt64(); }
    private sealed class SByteReader : ContentTypeReader<sbyte> { protected internal override sbyte Read(ContentReader input, sbyte existingInstance) => input.ReadSByte(); }
    private sealed class SingleReader : ContentTypeReader<float> { protected internal override float Read(ContentReader input, float existingInstance) => input.ReadSingle(); }
    private sealed class StringReader : ContentTypeReader<string> { protected internal override string Read(ContentReader input, string existingInstance) => input.ReadString(); }
    private sealed class UInt16Reader : ContentTypeReader<ushort> { protected internal override ushort Read(ContentReader input, ushort existingInstance) => input.ReadUInt16(); }
    private sealed class UInt32Reader : ContentTypeReader<uint> { protected internal override uint Read(ContentReader input, uint existingInstance) => input.ReadUInt32(); }
    private sealed class UInt64Reader : ContentTypeReader<ulong> { protected internal override ulong Read(ContentReader input, ulong existingInstance) => input.ReadUInt64(); }
    private sealed class Vector2Reader : ContentTypeReader<Vector2> { protected internal override Vector2 Read(ContentReader input, Vector2 existingInstance) => input.ReadVector2(); }
    private sealed class Vector3Reader : ContentTypeReader<Vector3> { protected internal override Vector3 Read(ContentReader input, Vector3 existingInstance) => input.ReadVector3(); }
    private sealed class Vector4Reader : ContentTypeReader<Vector4> { protected internal override Vector4 Read(ContentReader input, Vector4 existingInstance) => input.ReadVector4(); }
    private sealed class MatrixReader : ContentTypeReader<Matrix> { protected internal override Matrix Read(ContentReader input, Matrix existingInstance) => input.ReadMatrix(); }
    private sealed class QuaternionReader : ContentTypeReader<Quaternion> { protected internal override Quaternion Read(ContentReader input, Quaternion existingInstance) => input.ReadQuaternion(); }
    private sealed class ColorReader : ContentTypeReader<Color> { protected internal override Color Read(ContentReader input, Color existingInstance) => input.ReadColor(); }
}
