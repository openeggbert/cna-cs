namespace CNA.Content.Xnb;

using CNA.Graphics;

/// <summary>One built-in content type reader: how to read it, and what it targets.</summary>
/// <remarks>
/// <para>
/// <see cref="TargetIsValueType"/> is not bookkeeping. It decides how a *collection* reads its
/// elements: XNA's <c>ReadObject&lt;T&gt;(typeReader)</c> reads a value type inline with the reader
/// it already has, and a reference type through the polymorphic route with its own type-index
/// prefix. Get it wrong in either direction and every byte after the first element is misread.
/// </para>
/// <para>
/// It is *derived* from <paramref name="TargetType"/> rather than declared alongside it. It used to
/// be a separate boolean passed at each entry, which is a fact about the type restated by hand at
/// every call site -- exactly the shape that drifts. There is now one source.
/// </para>
/// <para>
/// <paramref name="TargetType"/> also lets a collection reader build the collection XNA would have
/// built. A <c>Dictionary&lt;string, object&gt;</c> read as <c>Dictionary&lt;object, object&gt;</c>
/// materialises without error and then fails the cast the game writes -- a load that succeeds into
/// an object the game cannot use is worse than one that fails.
/// </para>
/// </remarks>
internal readonly record struct XnbBuiltInReader(
    Func<XnbContentReader, object?> Read,
    Type TargetType)
{
    internal bool TargetIsValueType => TargetType.IsValueType;
}

/// <summary>
/// XNA's built-in content type readers, by the name the <c>.xnb</c> type-reader table spells and by
/// the content type each targets.
///
/// <b>Why both keys.</b> An asset's reader table names readers, so reading the root object needs a
/// reader-name lookup. A generic reader names its *element type* -- <c>ListReader`1[[System.Int32]]</c>
/// -- and has to find that element's reader, which is a type-name lookup. XNA does the same thing
/// through <c>ContentTypeReaderManager.GetTypeReader(Type)</c>; here the names are all that survive
/// the file, so the two tables are keyed by name.
///
/// Every format below is transcribed from the decompiled XNA 4.0 reader of the same name, not
/// guessed from the shape of the type. Two are worth naming because guessing would have been
/// plausible and wrong: <c>DateTimeReader</c> packs the kind into the top two bits of the tick
/// count, and <c>ColorReader</c> reads one packed <c>uint</c> rather than four bytes -- the same
/// four bytes, but a reader written from the struct's field order would still have been a guess.
/// </summary>
internal static class XnbBuiltInReaders
{
    private const string Prefix = "Microsoft.Xna.Framework.Content.";

    private static readonly (string Reader, string Target, XnbBuiltInReader Entry)[] Entries =
    [
        // -- primitives -------------------------------------------------------------------------
        Entry<byte>("ByteReader", "System.Byte", r => r.ReadByteValue()),
        Entry<sbyte>("SByteReader", "System.SByte", r => r.ReadSByteValue()),
        Entry<short>("Int16Reader", "System.Int16", r => r.ReadInt16Value()),
        Entry<ushort>("UInt16Reader", "System.UInt16", r => r.ReadUInt16Value()),
        Entry<int>("Int32Reader", "System.Int32", r => r.ReadInt32()),
        Entry<uint>("UInt32Reader", "System.UInt32", r => r.ReadUInt32()),
        Entry<long>("Int64Reader", "System.Int64", r => r.ReadInt64Value()),
        Entry<ulong>("UInt64Reader", "System.UInt64", r => r.ReadUInt64Value()),
        Entry<float>("SingleReader", "System.Single", r => r.ReadSingle()),
        Entry<double>("DoubleReader", "System.Double", r => r.ReadDoubleValue()),
        Entry<bool>("BooleanReader", "System.Boolean", r => r.ReadBoolean()),
        Entry<char>("CharReader", "System.Char", r => r.ReadChar()),
        Entry<decimal>("DecimalReader", "System.Decimal", r => r.ReadDecimalValue()),
        Entry<DateTime>("DateTimeReader", "System.DateTime", r => r.ReadDateTimeValue()),
        Entry<TimeSpan>("TimeSpanReader", "System.TimeSpan", r => r.ReadTimeSpanValue()),

        // The two built-ins whose target is a reference type, which is why the flag exists.
        Entry<string>("StringReader", "System.String", r => r.ReadString()),

        // System.Object never reads anything. XNA's own ObjectReader.Read throws
        // NotSupportedException, and that is not a gap: an object-typed slot is always polymorphic,
        // so this entry exists purely to answer "is the target a value type" with "no" and send the
        // read down the type-index route. Registering it is what makes Dictionary<string, object>
        // -- the shape XNA's own EffectMaterial parameters use -- resolvable at all.
        //
        // Reaching Read means a file named ObjectReader where a concrete reader belongs, so it
        // reports that rather than returning null and letting the caller misread what follows.
        Entry<object>("ObjectReader", "System.Object",
            static _ => throw new ContentLoadException(
                "Corrupt .xnb file: ObjectReader was reached directly. It only ever selects another " +
                "reader, so a file that reads through it names no concrete type for the value.")),

        // -- math -------------------------------------------------------------------------------
        Entry<Vector2>("Vector2Reader", "Microsoft.Xna.Framework.Vector2", r => r.ReadVector2Value()),
        Entry<Vector3>("Vector3Reader", "Microsoft.Xna.Framework.Vector3", r => r.ReadVector3()),
        Entry<Vector4>("Vector4Reader", "Microsoft.Xna.Framework.Vector4", r => r.ReadVector4Value()),
        Entry<Quaternion>("QuaternionReader", "Microsoft.Xna.Framework.Quaternion", r => r.ReadQuaternionValue()),
        Entry<Matrix>("MatrixReader", "Microsoft.Xna.Framework.Matrix", r => r.ReadMatrix()),
        Entry<Color>("ColorReader", "Microsoft.Xna.Framework.Color", r => r.ReadColorValue()),
        Entry<Point>("PointReader", "Microsoft.Xna.Framework.Point", r => r.ReadPointValue()),
        Entry<Rectangle>("RectangleReader", "Microsoft.Xna.Framework.Rectangle", r => r.ReadRectangle()),
        Entry<Plane>("PlaneReader", "Microsoft.Xna.Framework.Plane", r => r.ReadPlaneValue()),
        Entry<Ray>("RayReader", "Microsoft.Xna.Framework.Ray", r => r.ReadRayValue()),
        Entry<BoundingBox>("BoundingBoxReader", "Microsoft.Xna.Framework.BoundingBox", r => r.ReadBoundingBoxValue()),
        Entry<BoundingSphere>("BoundingSphereReader", "Microsoft.Xna.Framework.BoundingSphere", r => r.ReadBoundingSphere()),
        Entry<BoundingFrustum>("BoundingFrustumReader", "Microsoft.Xna.Framework.BoundingFrustum",
            r => new BoundingFrustum(r.ReadMatrix())),
        Entry<Curve>("CurveReader", "Microsoft.Xna.Framework.Curve", r => r.ReadCurveValue()),
    ];

    /// <summary>Reader name -> how to read it.</summary>
    /// <remarks>Declared after <c>Entries</c> deliberately: static field and auto-property
    /// initializers run in declaration order, so a table built before the array it reads gets a
    /// null and fails the whole type's initializer.</remarks>
    internal static IReadOnlyDictionary<string, XnbBuiltInReader> ByReaderName { get; } = Build();

    /// <summary>Content type name -> the reader that targets it. See <see cref="ByReaderName"/>
    /// for why the order of these declarations matters.</summary>
    internal static IReadOnlyDictionary<string, XnbBuiltInReader> ByTargetType { get; } = BuildByTarget();

    /// <summary>One entry. <paramref name="target"/> is the name the file spells and
    /// <typeparamref name="T"/> is the CLR type it means; keeping both, rather than deriving the
    /// name from the type, is what lets an entry name a type this assembly spells differently from
    /// the way the .xnb file does.</summary>
    private static (string, string, XnbBuiltInReader) Entry<T>(
        string reader, string target, Func<XnbContentReader, object?> read) =>
        (Prefix + reader, target, new XnbBuiltInReader(read, typeof(T)));

    private static Dictionary<string, XnbBuiltInReader> Build()
    {
        var map = new Dictionary<string, XnbBuiltInReader>(StringComparer.Ordinal);
        foreach ((string reader, _, XnbBuiltInReader entry) in Entries)
        {
            map[reader] = entry;
        }

        return map;
    }

    private static Dictionary<string, XnbBuiltInReader> BuildByTarget()
    {
        var map = new Dictionary<string, XnbBuiltInReader>(StringComparer.Ordinal);
        foreach ((_, string target, XnbBuiltInReader entry) in Entries)
        {
            map[target] = entry;
        }

        return map;
    }
}
