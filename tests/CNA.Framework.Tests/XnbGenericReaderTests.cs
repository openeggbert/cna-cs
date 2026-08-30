using CNA.Content;
using CNA.Content.Xnb;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// The generic built-in readers, exercised through the names a real <c>.xnb</c> type-reader table
/// actually contains after <see cref="XnbContentReader.NormalizeTypeReaderName"/> has run.
///
/// That last clause is the whole point of this file. A hand-written generic reader name looks like
/// <c>DictionaryReader`2[[System.String],[System.Int32]]</c> -- comma-separated, the way C# spells
/// a generic type. What a real file produces after assembly qualification is stripped is
/// <c>DictionaryReader`2[[System.String][System.Int32]]</c>, with **no comma**, because the comma
/// separating the two arguments is the same character that begins the first argument's assembly
/// qualification and is eaten with it. A resolver tested only against the hand-written spelling
/// passes while every real two-argument asset fails.
/// </summary>
public sealed class XnbGenericReaderTests
{
    /// <summary>The exact normalised spelling a real asset yields, derived by running the real
    /// normaliser over the real assembly-qualified name rather than by writing down what it is
    /// assumed to produce.</summary>
    [Theory]
    [InlineData(
        "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553",
        "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]")]
    [InlineData(
        "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]], Microsoft.Xna.Framework, Version=4.0.0.0",
        "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3]]")]
    public void Normalize_ProducesTheSpellingWithoutTheArgumentComma(string raw, string expected) =>
        Assert.Equal(expected, XnbContentReader.NormalizeTypeReaderName(raw));

    [Theory]
    // One argument: the case that always worked.
    [InlineData("Microsoft.Xna.Framework.Content.ListReader`1[[System.Int32]]")]
    [InlineData("Microsoft.Xna.Framework.Content.ArrayReader`1[[Microsoft.Xna.Framework.Vector2]]")]
    [InlineData("Microsoft.Xna.Framework.Content.NullableReader`1[[System.Char]]")]
    // Two arguments in the real, comma-free spelling.
    [InlineData("Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Int32]]")]
    [InlineData("Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]")]
    // A generic argument that is itself generic.
    [InlineData("Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Collections.Generic.List`1[[Microsoft.Xna.Framework.Vector3]]]]")]
    [InlineData("Microsoft.Xna.Framework.Content.ListReader`1[[System.Collections.Generic.List`1[[System.Int32]]]]")]
    public void TryResolve_ResolvesEveryRealSpelling(string readerName) =>
        Assert.NotNull(XnbGenericReaders.TryResolve(readerName));

    /// <summary>An element type nothing can read still fails by name. Falling back to the
    /// polymorphic route for an unknown element would read a plausible object graph out of
    /// misaligned bytes.</summary>
    [Theory]
    [InlineData("Microsoft.Xna.Framework.Content.ListReader`1[[Contoso.Game.Level]]")]
    [InlineData("Microsoft.Xna.Framework.Content.DictionaryReader`2[[Contoso.Game.Key][System.Int32]]")]
    [InlineData("Microsoft.Xna.Framework.Content.SomethingElseReader`1[[System.Int32]]")]
    public void TryResolve_UnknownElementType_IsNotResolved(string readerName) =>
        Assert.Null(XnbGenericReaders.TryResolve(readerName));

    /// <summary>
    /// The bytes, not just the resolution. A <c>Dictionary&lt;string, object&gt;</c> writes a
    /// 32-bit count and then, for every entry, a polymorphic key and a polymorphic value -- both
    /// carrying their own type-reader index, because <c>string</c> and <c>object</c> are reference
    /// types. Reading either one inline instead would consume the wrong number of bytes and
    /// misread every entry after the first, so the assertion is on the entries' values.
    /// </summary>
    [Fact]
    public void DictionaryOfStringToObject_ReadsKeysAndValues()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders:
            [
                "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]",
                "Microsoft.Xna.Framework.Content.StringReader",
                "Microsoft.Xna.Framework.Content.SingleReader",
                "Microsoft.Xna.Framework.Content.Vector3Reader",
            ],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);          // root: the dictionary
                writer.Write(2);                        // entry count
                writer.Write7BitEncodedInt(2);          // key: StringReader
                writer.Write("Alpha");
                writer.Write7BitEncodedInt(3);          // value: SingleReader
                writer.Write(2.5f);
                writer.Write7BitEncodedInt(2);          // key: StringReader
                writer.Write("Beta");
                writer.Write7BitEncodedInt(4);          // value: Vector3Reader
                writer.Write(1f);
                writer.Write(2f);
                writer.Write(3f);
            });

        // Dictionary<string, object>, exactly as XNA builds it -- not a Dictionary<object, object>
        // of boxed entries. The type is asserted because it is what a game casts to, and a load
        // that produces the wrong container type succeeds here and throws in the game.
        var read = Assert.IsType<Dictionary<string, object>>(XnbAssetWriter.ReadRoot(asset));

        Assert.Equal(2, read.Count);
        Assert.Equal(2.5f, Assert.IsType<float>(read["Alpha"]));
        Assert.Equal(new Vector3(1f, 2f, 3f), Assert.IsType<Vector3>(read["Beta"]));
    }

    /// <summary>A <c>Dictionary&lt;string, List&lt;Vector3&gt;&gt;</c>: the value's own reader is
    /// generic and reached through the value type's name rather than through a reader name.</summary>
    [Fact]
    public void DictionaryOfStringToListOfVector3_ReadsNestedLists()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders:
            [
                "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Collections.Generic.List`1[[Microsoft.Xna.Framework.Vector3]]]]",
                "Microsoft.Xna.Framework.Content.StringReader",
                "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3]]",
            ],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write(1);                        // one entry
                writer.Write7BitEncodedInt(2);          // key
                writer.Write("path");
                writer.Write7BitEncodedInt(3);          // value: the list
                writer.Write(2);                        // two elements
                writer.Write(1f); writer.Write(2f); writer.Write(3f);
                writer.Write(4f); writer.Write(5f); writer.Write(6f);
            });

        var read = Assert.IsType<Dictionary<string, List<Vector3>>>(XnbAssetWriter.ReadRoot(asset));

        // List<Vector3>, not List<object>: XnbContentReader's own table has an explicit entry for
        // this one instantiation, because SpriteFont's kerning list needs a strongly typed result,
        // and reader-name lookup consults that table before the generic resolver. Both read the
        // same bytes -- three inline floats per element -- so the shadowing changes the element
        // container's type and nothing about the parse. Asserted as it actually is rather than as
        // the generic path alone would produce, so that removing the shadowing entry shows up here.
        var points = Assert.IsType<List<Vector3>>(read["path"]);

        Assert.Equal(new Vector3(1f, 2f, 3f), points[0]);
        Assert.Equal(new Vector3(4f, 5f, 6f), points[1]);
    }

    /// <summary>
    /// <c>System.Object</c> resolves as a *dispatch* target and never reads anything itself. XNA's
    /// own <c>ObjectReader.Read</c> throws <c>NotSupportedException</c> for exactly this reason: an
    /// <c>object</c>-typed slot is always polymorphic, so the reader is only ever consulted for
    /// "is this a value type" and the answer routes the read elsewhere. A file that reaches it
    /// directly -- an asset whose *root* reader is ObjectReader -- is malformed, and says so.
    /// </summary>
    [Fact]
    public void ObjectReader_AsARootReader_IsRefusedRatherThanReadingZeroBytes()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders: ["Microsoft.Xna.Framework.Content.ObjectReader"],
            writeRoot: writer => writer.Write7BitEncodedInt(1));

        ContentLoadException failure = Assert.Throws<ContentLoadException>(() => XnbAssetWriter.ReadRoot(asset));
        Assert.Contains("ObjectReader", failure.Message, StringComparison.Ordinal);
    }
}
