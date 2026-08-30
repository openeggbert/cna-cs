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
            if (versions[index] != readers[index].TypeVersion)
            {
                throw new ContentLoadException(
                    $"Content asset '{input.AssetName}' has an incompatible version for reader " +
                    $"'{readers[index].TargetType}'.");
            }

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

    /// <summary>
    /// Whether a serialized reader name resolves to something this binding can construct.
    ///
    /// Exists for <c>tools/content-survey</c>, which measures how much of a real game's compiled
    /// content is readable. Asking the same code the loader asks is the point: a survey that
    /// reimplemented the lookup would drift from it, and a drifted survey is worse than none
    /// because it reports a number nobody can act on.
    /// </summary>
    internal static bool CanResolveForSurvey(string serializedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedName);

        try
        {
            _ = CreateReader(serializedName, "(survey)");
            return true;
        }
        catch (ContentLoadException)
        {
            return false;
        }
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
            "Microsoft.Xna.Framework.Content.DecimalReader" => new DecimalReader(),
            "Microsoft.Xna.Framework.Content.DateTimeReader" => new DateTimeReader(),
            "Microsoft.Xna.Framework.Content.TimeSpanReader" => new TimeSpanReader(),
            "Microsoft.Xna.Framework.Content.PointReader" => new PointReader(),
            "Microsoft.Xna.Framework.Content.RectangleReader" => new RectangleReader(),
            "Microsoft.Xna.Framework.Content.PlaneReader" => new PlaneReader(),
            "Microsoft.Xna.Framework.Content.RayReader" => new RayReader(),
            "Microsoft.Xna.Framework.Content.BoundingBoxReader" => new BoundingBoxReader(),
            "Microsoft.Xna.Framework.Content.BoundingSphereReader" => new BoundingSphereReader(),
            "Microsoft.Xna.Framework.Content.BoundingFrustumReader" => new BoundingFrustumReader(),
            "Microsoft.Xna.Framework.Content.CurveReader" => new CurveReader(),
            "Microsoft.Xna.Framework.Content.ExternalReferenceReader" => new ExternalReferenceReader(),

            // The texture readers, for a texture nested inside another asset. A top-level
            // Load<Texture2D> never reaches here -- it goes to CNA's own content loader.
            "Microsoft.Xna.Framework.Content.Texture2DReader" => new Texture2DContentReader(),
            "Microsoft.Xna.Framework.Content.TextureCubeReader" => new TextureCubeContentReader(),
            "Microsoft.Xna.Framework.Content.Texture3DReader" => new Texture3DContentReader(),

            // A compiled song or video is a path plus the metadata the pipeline measured, not the
            // media itself. SongReader is the root reader of every Load<Song>, so a game with
            // background music failed at the call rather than in some nested corner.
            "Microsoft.Xna.Framework.Content.SongReader" => new SongContentReader(),
            "Microsoft.Xna.Framework.Content.VideoReader" => new VideoContentReader(),

            // The model pipeline, for a model nested inside another asset. A top-level
            // Load<Model> goes to CNA's own loader and never reaches these.
            "Microsoft.Xna.Framework.Content.ModelReader" => new ModelContentReader(),
            "Microsoft.Xna.Framework.Content.VertexDeclarationReader" => new VertexDeclarationContentReader(),
            "Microsoft.Xna.Framework.Content.VertexBufferReader" => new VertexBufferContentReader(),
            "Microsoft.Xna.Framework.Content.IndexBufferReader" => new IndexBufferContentReader(),
            "Microsoft.Xna.Framework.Content.TextureReader" => new AbstractTextureContentReader(),
            "Microsoft.Xna.Framework.Content.EffectReader" => new EffectContentReader(),
            "Microsoft.Xna.Framework.Content.BasicEffectReader" => new BasicEffectContentReader(),
            _ => BuiltinGenericReaders.TryCreate(serializedName),
        };
        return reader is not null;
    }

    /// <summary>
    /// The reader for a target type, for a collection reader whose element reader is not in the
    /// asset's own table.
    ///
    /// XNA's writer does emit it, so this is a fallback rather than the normal path -- but a
    /// fallback that answers is worth more than one that throws, and the alternative is failing an
    /// otherwise-loadable asset over a table entry the pipeline chose not to duplicate.
    /// </summary>
    internal static ContentTypeReader? TryCreateForTargetType(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (targetType.IsEnum)
        {
            return (ContentTypeReader?)Activator.CreateInstance(
                typeof(EnumReader<>).MakeGenericType(targetType), nonPublic: true);
        }

        string? readerName = ReaderNameForTargetType(targetType);
        return readerName is not null && TryCreate(readerName, out ContentTypeReader? reader) ? reader : null;
    }

    private static string? ReaderNameForTargetType(Type targetType)
    {
        const string prefix = "Microsoft.Xna.Framework.Content.";
        string? simple = targetType switch
        {
            _ when targetType == typeof(bool) => "BooleanReader",
            _ when targetType == typeof(byte) => "ByteReader",
            _ when targetType == typeof(sbyte) => "SByteReader",
            _ when targetType == typeof(char) => "CharReader",
            _ when targetType == typeof(short) => "Int16Reader",
            _ when targetType == typeof(ushort) => "UInt16Reader",
            _ when targetType == typeof(int) => "Int32Reader",
            _ when targetType == typeof(uint) => "UInt32Reader",
            _ when targetType == typeof(long) => "Int64Reader",
            _ when targetType == typeof(ulong) => "UInt64Reader",
            _ when targetType == typeof(float) => "SingleReader",
            _ when targetType == typeof(double) => "DoubleReader",
            _ when targetType == typeof(decimal) => "DecimalReader",
            _ when targetType == typeof(string) => "StringReader",
            _ when targetType == typeof(DateTime) => "DateTimeReader",
            _ when targetType == typeof(TimeSpan) => "TimeSpanReader",
            _ when targetType == typeof(Vector2) => "Vector2Reader",
            _ when targetType == typeof(Vector3) => "Vector3Reader",
            _ when targetType == typeof(Vector4) => "Vector4Reader",
            _ when targetType == typeof(Matrix) => "MatrixReader",
            _ when targetType == typeof(Quaternion) => "QuaternionReader",
            _ when targetType == typeof(Color) => "ColorReader",
            _ when targetType == typeof(Point) => "PointReader",
            _ when targetType == typeof(Rectangle) => "RectangleReader",
            _ when targetType == typeof(Plane) => "PlaneReader",
            _ when targetType == typeof(Ray) => "RayReader",
            _ when targetType == typeof(BoundingBox) => "BoundingBoxReader",
            _ when targetType == typeof(BoundingSphere) => "BoundingSphereReader",
            _ when targetType == typeof(BoundingFrustum) => "BoundingFrustumReader",
            _ when targetType == typeof(Curve) => "CurveReader",
            _ when targetType == typeof(Graphics.Texture2D) => "Texture2DReader",
            _ when targetType == typeof(Graphics.TextureCube) => "TextureCubeReader",
            _ when targetType == typeof(Graphics.Texture3D) => "Texture3DReader",
            _ when targetType == typeof(Graphics.VertexDeclaration) => "VertexDeclarationReader",
            _ when targetType == typeof(Graphics.VertexBuffer) => "VertexBufferReader",
            _ when targetType == typeof(Graphics.IndexBuffer) => "IndexBufferReader",
            _ when targetType == typeof(Graphics.Model) => "ModelReader",
            _ => null,
        };

        return simple is null ? null : prefix + simple;
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

    // The rest of XNA's value readers, transcribed from the decompiled 4.0 readers. Two of them are
    // why transcription beats inference: DateTime packs its Kind into the top two bits of the tick
    // count, and Decimal is four Int32 bits words in constructor order.
    private sealed class DecimalReader : ContentTypeReader<decimal>
    {
        protected internal override decimal Read(ContentReader input, decimal existingInstance) =>
            new([input.ReadInt32(), input.ReadInt32(), input.ReadInt32(), input.ReadInt32()]);
    }

    private sealed class DateTimeReader : ContentTypeReader<DateTime>
    {
        protected internal override DateTime Read(ContentReader input, DateTime existingInstance)
        {
            long packed = input.ReadInt64();
            long ticks = packed & 0x3FFFFFFFFFFFFFFFL;
            var kind = (DateTimeKind)(int)((ulong)packed >> 62);
            return kind == DateTimeKind.Local
                ? new DateTime(ticks, DateTimeKind.Utc).ToLocalTime()
                : new DateTime(ticks, kind);
        }
    }

    private sealed class TimeSpanReader : ContentTypeReader<TimeSpan>
    {
        protected internal override TimeSpan Read(ContentReader input, TimeSpan existingInstance) =>
            TimeSpan.FromTicks(input.ReadInt64());
    }

    private sealed class PointReader : ContentTypeReader<Point>
    {
        protected internal override Point Read(ContentReader input, Point existingInstance) =>
            new(input.ReadInt32(), input.ReadInt32());
    }

    private sealed class RectangleReader : ContentTypeReader<Rectangle>
    {
        protected internal override Rectangle Read(ContentReader input, Rectangle existingInstance) =>
            new(input.ReadInt32(), input.ReadInt32(), input.ReadInt32(), input.ReadInt32());
    }

    private sealed class PlaneReader : ContentTypeReader<Plane>
    {
        protected internal override Plane Read(ContentReader input, Plane existingInstance) =>
            new(input.ReadVector3(), input.ReadSingle());
    }

    private sealed class RayReader : ContentTypeReader<Ray>
    {
        protected internal override Ray Read(ContentReader input, Ray existingInstance) =>
            new(input.ReadVector3(), input.ReadVector3());
    }

    private sealed class BoundingBoxReader : ContentTypeReader<BoundingBox>
    {
        protected internal override BoundingBox Read(ContentReader input, BoundingBox existingInstance) =>
            new(input.ReadVector3(), input.ReadVector3());
    }

    private sealed class BoundingSphereReader : ContentTypeReader<BoundingSphere>
    {
        protected internal override BoundingSphere Read(ContentReader input, BoundingSphere existingInstance) =>
            new(input.ReadVector3(), input.ReadSingle());
    }

    private sealed class BoundingFrustumReader : ContentTypeReader<BoundingFrustum>
    {
        protected internal override BoundingFrustum Read(ContentReader input, BoundingFrustum existingInstance) =>
            new(input.ReadMatrix());
    }

    private sealed class CurveReader : ContentTypeReader<Curve>
    {
        public override bool CanDeserializeIntoExistingObject => true;

        protected internal override Curve Read(ContentReader input, Curve existingInstance)
        {
            Curve curve = existingInstance ?? new Curve();
            curve.PreLoop = (CurveLoopType)input.ReadInt32();
            curve.PostLoop = (CurveLoopType)input.ReadInt32();

            int count = input.ReadInt32();
            if (count is < 0 or > 10_000_000)
            {
                throw new ContentLoadException(
                    $"Content asset '{input.AssetName}' declares an implausible curve key count {count}.");
            }

            for (int index = 0; index < count; index++)
            {
                curve.Keys.Add(new CurveKey(
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle(),
                    (CurveContinuity)input.ReadInt32()));
            }

            return curve;
        }
    }

    /// <summary>
    /// An external reference is a path the manager loads as its own asset, so the target type is
    /// whatever the caller asked for.
    ///
    /// It derives from the non-generic base rather than <c>ContentTypeReader&lt;object&gt;</c>:
    /// that closure makes the generic base's two <c>Read</c> overloads collapse into the same
    /// signature, which the compiler rejects.
    /// </summary>
    private sealed class ExternalReferenceReader : ContentTypeReader
    {
        internal ExternalReferenceReader()
            : base(typeof(object))
        {
        }

        protected internal override object Read(ContentReader input, object? existingInstance)
        {
            ArgumentNullException.ThrowIfNull(input);
            return input.ReadExternalReference<object>();
        }
    }
}
