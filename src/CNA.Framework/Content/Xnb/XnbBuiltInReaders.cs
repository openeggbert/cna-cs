namespace CNA.Content.Xnb;

using CNA.Graphics;

/// <summary>One built-in content type reader: how to read it, and whether its target is a value type.</summary>
/// <remarks>
/// <paramref name="TargetIsValueType"/> is not bookkeeping. It decides how a *collection* reads its
/// elements: XNA's <c>ReadObject&lt;T&gt;(typeReader)</c> reads a value type inline with the reader
/// it already has, and a reference type through the polymorphic route with its own type-index
/// prefix. Get it wrong in either direction and every byte after the first element is misread.
/// </remarks>
internal readonly record struct XnbBuiltInReader(
    Func<XnbContentReader, object?> Read,
    bool TargetIsValueType);

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
        Value("ByteReader", "System.Byte", r => r.ReadByteValue()),
        Value("SByteReader", "System.SByte", r => r.ReadSByteValue()),
        Value("Int16Reader", "System.Int16", r => r.ReadInt16Value()),
        Value("UInt16Reader", "System.UInt16", r => r.ReadUInt16Value()),
        Value("Int32Reader", "System.Int32", r => r.ReadInt32()),
        Value("UInt32Reader", "System.UInt32", r => r.ReadUInt32()),
        Value("Int64Reader", "System.Int64", r => r.ReadInt64Value()),
        Value("UInt64Reader", "System.UInt64", r => r.ReadUInt64Value()),
        Value("SingleReader", "System.Single", r => r.ReadSingle()),
        Value("DoubleReader", "System.Double", r => r.ReadDoubleValue()),
        Value("BooleanReader", "System.Boolean", r => r.ReadBoolean()),
        Value("CharReader", "System.Char", r => r.ReadChar()),
        Value("DecimalReader", "System.Decimal", r => r.ReadDecimalValue()),
        Value("DateTimeReader", "System.DateTime", r => r.ReadDateTimeValue()),
        Value("TimeSpanReader", "System.TimeSpan", r => r.ReadTimeSpanValue()),

        // The one built-in whose target is a reference type, which is why the flag exists.
        Reference("StringReader", "System.String", r => r.ReadString()),

        // -- math -------------------------------------------------------------------------------
        Value("Vector2Reader", "Microsoft.Xna.Framework.Vector2", r => r.ReadVector2Value()),
        Value("Vector3Reader", "Microsoft.Xna.Framework.Vector3", r => r.ReadVector3()),
        Value("Vector4Reader", "Microsoft.Xna.Framework.Vector4", r => r.ReadVector4Value()),
        Value("QuaternionReader", "Microsoft.Xna.Framework.Quaternion", r => r.ReadQuaternionValue()),
        Value("MatrixReader", "Microsoft.Xna.Framework.Matrix", r => r.ReadMatrix()),
        Value("ColorReader", "Microsoft.Xna.Framework.Color", r => r.ReadColorValue()),
        Value("PointReader", "Microsoft.Xna.Framework.Point", r => r.ReadPointValue()),
        Value("RectangleReader", "Microsoft.Xna.Framework.Rectangle", r => r.ReadRectangle()),
        Value("PlaneReader", "Microsoft.Xna.Framework.Plane", r => r.ReadPlaneValue()),
        Value("RayReader", "Microsoft.Xna.Framework.Ray", r => r.ReadRayValue()),
        Value("BoundingBoxReader", "Microsoft.Xna.Framework.BoundingBox", r => r.ReadBoundingBoxValue()),
        Value("BoundingSphereReader", "Microsoft.Xna.Framework.BoundingSphere", r => r.ReadBoundingSphere()),
        Reference("BoundingFrustumReader", "Microsoft.Xna.Framework.BoundingFrustum",
            r => new BoundingFrustum(r.ReadMatrix())),
        Reference("CurveReader", "Microsoft.Xna.Framework.Curve", r => r.ReadCurveValue()),
    ];

    /// <summary>Reader name -> how to read it.</summary>
    /// <remarks>Declared after <c>Entries</c> deliberately: static field and auto-property
    /// initializers run in declaration order, so a table built before the array it reads gets a
    /// null and fails the whole type's initializer.</remarks>
    internal static IReadOnlyDictionary<string, XnbBuiltInReader> ByReaderName { get; } = Build();

    /// <summary>Content type name -> the reader that targets it. See <see cref="ByReaderName"/>
    /// for why the order of these declarations matters.</summary>
    internal static IReadOnlyDictionary<string, XnbBuiltInReader> ByTargetType { get; } = BuildByTarget();

    private static (string, string, XnbBuiltInReader) Value(
        string reader, string target, Func<XnbContentReader, object?> read) =>
        (Prefix + reader, target, new XnbBuiltInReader(read, TargetIsValueType: true));

    private static (string, string, XnbBuiltInReader) Reference(
        string reader, string target, Func<XnbContentReader, object?> read) =>
        (Prefix + reader, target, new XnbBuiltInReader(read, TargetIsValueType: false));

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
