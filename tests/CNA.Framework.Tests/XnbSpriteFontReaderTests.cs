using CNA.Content;
using CNA.Content.Xnb;
using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// Parses the two real, LZX-compressed <c>.xnb</c> fixtures this suite already ships -- a
/// <c>Texture2D</c> and a <c>SpriteFont</c> -- through the readers added when the fabricated
/// <c>cna_content_load_spritefont</c> P/Invoke was removed.
///
/// These fixtures were previously used only to prove that decompression produced *well-formed*
/// bytes, by asserting the reader then failed cleanly on an unregistered root type. Now that both
/// root types are registered, they check the parse itself, which is a stronger claim about the same
/// files: real content-pipeline output, read end to end, with no native library involved.
/// </summary>
public class XnbSpriteFontReaderTests
{
    private static readonly string AssetsDirectory = Path.Combine("assets", "xnb", "lzx");

    private static object? ParseFixture(string fixtureName)
    {
        byte[] fileBytes = File.ReadAllBytes(Path.Combine(AssetsDirectory, fixtureName + ".xnb"));
        using var reader = new BinaryReader(new MemoryStream(fileBytes));
        XnbHeader header = XnbHeader.Read(reader, fileBytes.Length);

        int decompressedSize = reader.ReadInt32();
        int compressedSize = header.TotalLength - XnbHeader.LzxPayloadOffset;
        byte[] compressed = reader.ReadBytes(compressedSize);
        byte[] decompressed = XnbLzxDecompression.Decompress(compressed, decompressedSize, fixtureName);

        XnbContentReader contentReader = XnbContentReader.Create(new BinaryReader(new MemoryStream(decompressed)));
        return contentReader.ReadRootObjectAndResolveSharedResources();
    }

    [Fact]
    public void Texture2DFixture_ParsesToRealDimensionsAndPixelData()
    {
        var texture = Assert.IsType<XnbTextureData>(ParseFixture("Explosion"));

        Assert.True(texture.Width > 0);
        Assert.True(texture.Height > 0);
        Assert.NotEmpty(texture.MipLevels);

        // Level 0 must hold exactly one 32-bit pixel per texel. A reader that mis-parsed the
        // dimensions or the level byte count would still produce *some* bytes -- this is what
        // catches that.
        Assert.Equal(SurfaceFormat.Color, texture.Format);
        Assert.Equal(texture.Width * texture.Height * 4, texture.MipLevels[0].Length);
    }

    [Fact]
    public void SpriteFontFixture_ParsesToAConsistentGlyphTable()
    {
        var font = Assert.IsType<XnbSpriteFontData>(ParseFixture("FontCalibri14"));

        Assert.NotEmpty(font.Characters);

        // The four per-glyph lists are written as four separate objects, so nothing in the format
        // itself forces them to agree. Their agreeing is what makes the table usable, and reading
        // them in the wrong order is the mistake this catches.
        Assert.Equal(font.Characters.Count, font.GlyphBounds.Count);
        Assert.Equal(font.Characters.Count, font.Cropping.Count);
        Assert.Equal(font.Characters.Count, font.Kerning.Count);
    }

    [Fact]
    public void SpriteFontFixture_GlyphBoundsFitInsideItsAtlas()
    {
        var font = Assert.IsType<XnbSpriteFontData>(ParseFixture("FontCalibri14"));

        foreach (Rectangle bounds in font.GlyphBounds)
        {
            Assert.True(bounds.X >= 0 && bounds.Y >= 0, $"Glyph bounds {bounds} has a negative origin.");
            Assert.True(
                bounds.X + bounds.Width <= font.Texture.Width && bounds.Y + bounds.Height <= font.Texture.Height,
                $"Glyph bounds {bounds} falls outside the {font.Texture.Width}x{font.Texture.Height} atlas.");
        }
    }

    /// <summary>A font's character map is sorted ascending in a real <c>.xnb</c>, and
    /// <see cref="SpriteFont"/> relies on the characters being distinct to build its index. Reading
    /// the character list with the wrong element width -- <c>char</c> is two bytes, not one or four
    /// -- produces a list that fails both.</summary>
    [Fact]
    public void SpriteFontFixture_CharactersAreDistinctAndAscending()
    {
        var font = Assert.IsType<XnbSpriteFontData>(ParseFixture("FontCalibri14"));

        for (int i = 1; i < font.Characters.Count; i++)
        {
            Assert.True(
                font.Characters[i] > font.Characters[i - 1],
                $"Character map is not strictly ascending at index {i}: " +
                $"U+{(int)font.Characters[i - 1]:X4} then U+{(int)font.Characters[i]:X4}.");
        }
    }

    [Fact]
    public void SpriteFontFixture_HasPlausibleLayoutMetrics()
    {
        var font = Assert.IsType<XnbSpriteFontData>(ParseFixture("FontCalibri14"));

        // Read in the wrong order, LineSpacing and Spacing swap an int for a float and produce
        // wildly implausible values rather than merely wrong ones.
        Assert.InRange(font.LineSpacing, 1, 512);
        Assert.InRange(font.Spacing, -64f, 64f);
    }

    /// <summary>The generic type-reader names in a real font
    /// (<c>ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=...]]</c>)
    /// carry commas inside their brackets. Trimming at the first comma -- which is what this reader
    /// did before the font path existed -- truncates the element type and loses the match.</summary>
    [Theory]
    [InlineData("Microsoft.Xna.Framework.Content.SpriteFontReader, Microsoft.Xna.Framework, Version=4.0.0.0",
                "Microsoft.Xna.Framework.Content.SpriteFontReader")]
    [InlineData("Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral]]",
                "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle]]")]
    [InlineData("Microsoft.Xna.Framework.Content.NullableReader`1[[System.Char, mscorlib, Version=4.0.0.0]]",
                "Microsoft.Xna.Framework.Content.NullableReader`1[[System.Char]]")]
    [InlineData("Microsoft.Xna.Framework.Content.ModelReader", "Microsoft.Xna.Framework.Content.ModelReader")]
    public void NormalizeTypeReaderName_StripsAssemblyQualificationAtEveryBracketDepth(string raw, string expected)
    {
        Assert.Equal(expected, XnbContentReader.NormalizeTypeReaderName(raw));
    }
}
