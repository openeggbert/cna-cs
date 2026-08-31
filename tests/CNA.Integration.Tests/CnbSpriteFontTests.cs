using CNA.Content.Cnb;
using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// A compiled CNB sprite font, decoded and then made drawable.
///
/// The same two-step shape the CNB texture slice established: <see cref="CnbSpriteFont"/> is a
/// decoded description readable without a device, and <see cref="CnbSpriteFontLoader"/> is the step
/// that needs one. That split matters here more than it did for a texture, because a
/// <see cref="SpriteFont"/> is a glyph table *and* an atlas, and a game inspecting metrics --
/// measuring a string to size a dialog box, say -- should not have to create a device first.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbSpriteFontTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>
    /// Two glyphs with deliberately different metrics, and an atlas whose four texels are four
    /// different colours.
    ///
    /// Everything asymmetric: a font whose glyphs shared bounds, or whose atlas was one flat
    /// colour, would let a reader confuse the two glyphs or transpose the atlas and still pass.
    /// </summary>
    private static string WriteFixture(char? defaultCharacter = 'A')
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-font-{Guid.NewGuid():N}.cnb");

        using var builder = new CnbTestSpriteFontBuilder();
        builder.SetInfo(lineSpacing: 21, spacing: 1.5f, defaultCharacter);
        builder.AddGlyph('A', new Rectangle(0, 0, 1, 1), new Rectangle(1, 2, 3, 4), new Vector3(0.5f, 6f, 0.25f));
        builder.AddGlyph('B', new Rectangle(1, 0, 1, 1), new Rectangle(5, 6, 7, 8), new Vector3(1.5f, 9f, 0.75f));

        // 2x1 RGBA8: red then green, so a transposed or duplicated atlas is visible.
        builder.SetAtlas(2, 1, [255, 0, 0, 255, 0, 255, 0, 255]);
        builder.WriteToFile(path, "fonts/fixture");
        return path;
    }

    /// <summary>Identity and metrics, without a device.</summary>
    [NativeFact]
    public void DecodedFont_CarriesItsMetricsAndGlyphs()
    {
        string path = WriteFixture();
        try
        {
            using CnbSpriteFont font = CnbSpriteFont.DecodeFile(path);

            Assert.Equal(2, font.GlyphCount);
            Assert.Equal(21, font.LineSpacing);
            Assert.Equal(1.5f, font.Spacing);
            Assert.Equal('A', font.DefaultCharacter);

            CnbGlyph first = font.GetGlyph(0);
            CnbGlyph second = font.GetGlyph(1);

            Assert.Equal('A', first.Character);
            Assert.Equal('B', second.Character);

            // Every field of both glyphs. Bounds and cropping are separate rectangles that a reader
            // could easily swap, and they are different values here so a swap is visible.
            Assert.Equal(new Rectangle(0, 0, 1, 1), first.Bounds);
            Assert.Equal(new Rectangle(1, 2, 3, 4), first.Cropping);
            Assert.Equal(new Vector3(0.5f, 6f, 0.25f), first.Kerning);

            Assert.Equal(new Rectangle(1, 0, 1, 1), second.Bounds);
            Assert.Equal(new Rectangle(5, 6, 7, 8), second.Cropping);
            Assert.Equal(new Vector3(1.5f, 9f, 0.75f), second.Kerning);

            output.WriteLine($"{font.GlyphCount} glyphs, line spacing {font.LineSpacing}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A font that declares no default character reports <see langword="null"/>, not <c>'\0'</c>.
    ///
    /// CNA carries the presence flag separately from the value, and collapsing the two would turn
    /// XNA's "throw on a missing glyph" into "draw a null character" -- which no test of the value
    /// alone could distinguish, since both are the same two bytes.
    /// </summary>
    [NativeFact]
    public void DecodedFont_DistinguishesNoDefaultCharacterFromAZeroOne()
    {
        string withDefault = WriteFixture('A');
        string withoutDefault = WriteFixture(null);
        try
        {
            using CnbSpriteFont declared = CnbSpriteFont.DecodeFile(withDefault);
            using CnbSpriteFont absent = CnbSpriteFont.DecodeFile(withoutDefault);

            Assert.Equal('A', declared.DefaultCharacter);
            Assert.Null(absent.DefaultCharacter);
        }
        finally
        {
            File.Delete(withDefault);
            File.Delete(withoutDefault);
        }
    }

    /// <summary>
    /// The atlas is a copy, so it outlives the font it came from.
    ///
    /// CNA's route is named <c>copy_atlas</c> and documents its output as a new description; this
    /// asserts the binding treats it that way rather than as a borrow. A borrowed atlas read after
    /// its font was disposed is exactly the class of bug that does not announce itself.
    /// </summary>
    [NativeFact]
    public void CopiedAtlas_OutlivesTheFontItCameFrom()
    {
        string path = WriteFixture();
        try
        {
            CnbTexture atlas;
            using (CnbSpriteFont font = CnbSpriteFont.DecodeFile(path))
            {
                atlas = font.CopyAtlas();
            }

            using (atlas)
            {
                Assert.Equal(2, atlas.Width);
                Assert.Equal(1, atlas.Height);
                output.WriteLine($"atlas {atlas.Width}x{atlas.Height} readable after the font went away");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The whole point: a <c>.cnb</c> file becomes a drawable <see cref="SpriteFont"/> whose glyph
    /// table came from the file.
    ///
    /// <see cref="SpriteFont.MeasureString"/> is the assertion because it is a pure function of the
    /// glyph table, so it exercises the crossing rather than the upload -- a font built with the
    /// glyphs in the wrong order, or with cropping where bounds belong, measures differently.
    /// </summary>
    [NativeFact]
    public void CnbFile_BecomesADrawableSpriteFont()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            string path = WriteFixture();
            try
            {
                SpriteFont font = CnbSpriteFontLoader.LoadSpriteFont(device, path);

                Assert.Equal(21, font.LineSpacing);
                Assert.Equal(1.5f, font.Spacing);
                Assert.NotNull(font.Texture);

                // Both characters are in the table, so neither measurement throws -- which is what
                // XNA does for a character a font has no glyph for and no default.
                Vector2 a = font.MeasureString("A");
                Vector2 ab = font.MeasureString("AB");
                output.WriteLine($"MeasureString A={a} AB={ab}");

                Assert.True(ab.X > a.X, "Two glyphs must measure wider than one.");
                Assert.Equal(a.Y, ab.Y);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    /// <summary>A document that is not a sprite font is refused rather than decoded into an empty
    /// font.</summary>
    [NativeFact]
    public void Decode_RefusesADocumentThatIsNotASpriteFont()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-notafont-{Guid.NewGuid():N}.cnb");
        try
        {
            using (var writer = new CnbTestWriter(0x54534554, 1))
            {
                writer.AddChunk(CnbTestWriter.ChunkId("ONE_"), [1, 2, 3, 4]);
                writer.WriteToFile(path);
            }

            using CnbDocument document = CnbDocument.Open(path);
            CnaException failure = Assert.Throws<CnaException>(() => CnbSpriteFont.Decode(document));
            output.WriteLine($"non-font document refused: {failure.Message}");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
