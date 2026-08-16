using CNA;
using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// SpriteFont.MeasureString is pure managed code (see ../../src/CNA.Framework/Graphics/SpriteFont.cs)
/// -- these tests construct a small hand-built font (three glyphs, known kerning/cropping values)
/// and check MeasureString's output against numbers worked out by hand from the same "ABC"
/// kerning-triple algorithm the implementation uses, so a regression in the walk logic itself
/// would be caught even though nothing here touches native code. The dummy Texture2D is built
/// through the internal raw-handle constructor with handle value 0 (an *invalid* SafeHandle,
/// per NativeResourceHandle.IsInvalid), so disposal never calls into native code -- this test
/// never calls Texture2D.Width/Height/Dispose, which would.
/// </summary>
public class SpriteFontTests
{
    private static SpriteFont CreateTestFont(int lineSpacing = 12, float spacing = 2f)
    {
        var texture = new Texture2D(nativeHandleValue: 0);

        return new SpriteFont(
            texture,
            glyphBounds:
            [
                new Rectangle(0, 0, 10, 10),
                new Rectangle(10, 0, 10, 10),
                new Rectangle(20, 0, 4, 10),
            ],
            cropping:
            [
                new Rectangle(0, 0, 10, 10),
                new Rectangle(0, 0, 10, 10),
                new Rectangle(0, 0, 4, 10),
            ],
            characters: ['A', 'B', ' '],
            lineSpacing: lineSpacing,
            spacing: spacing,
            kerning:
            [
                new Vector3(0, 8, 0),
                new Vector3(1, 7, 1),
                new Vector3(0, 4, 0),
            ],
            defaultCharacter: null);
    }

    [Fact]
    public void MeasureString_EmptyString_ReturnsZero()
    {
        SpriteFont font = CreateTestFont();

        Assert.Equal(Vector2.Zero, font.MeasureString(string.Empty));
    }

    [Fact]
    public void MeasureString_SingleGlyph_UsesFullKerningAdvanceAndLineSpacing()
    {
        SpriteFont font = CreateTestFont();

        Vector2 size = font.MeasureString("A");

        Assert.Equal(8f, size.X);
        Assert.Equal(12f, size.Y);
    }

    [Fact]
    public void MeasureString_TwoGlyphs_AddsSpacingAndSecondGlyphsBearings()
    {
        SpriteFont font = CreateTestFont();

        Vector2 size = font.MeasureString("AB");

        // A: advance 8 (0 + 8 + 0). Then Spacing(2) + B.LeftSideBearing(1) = 3, + B.Width(7) = 18,
        // + B.RightSideBearing(1) = 19.
        Assert.Equal(19f, size.X);
        Assert.Equal(12f, size.Y);
    }

    [Fact]
    public void MeasureString_Newline_ResetsXAndAddsLineSpacingToY()
    {
        SpriteFont font = CreateTestFont();

        Vector2 size = font.MeasureString("A\nB");

        // Second line's "B" is first-in-line, so only max(LeftSideBearing, 0) = 1 applies before
        // its width: 1 + 7 + 1 = 9. Two lines of LineSpacing(12) each => Y = 24.
        Assert.Equal(9f, size.X);
        Assert.Equal(24f, size.Y);
    }

    [Fact]
    public void MeasureString_UnknownCharacterWithNoDefault_Throws()
    {
        SpriteFont font = CreateTestFont();

        Assert.Throws<ArgumentException>(() => font.MeasureString("Z"));
    }

    [Fact]
    public void MeasureString_UnknownCharacterWithDefault_FallsBackToDefaultCharacterMetrics()
    {
        var texture = new Texture2D(nativeHandleValue: 0);
        var font = new SpriteFont(
            texture,
            glyphBounds: [new Rectangle(0, 0, 10, 10)],
            cropping: [new Rectangle(0, 0, 10, 10)],
            characters: ['A'],
            lineSpacing: 12,
            spacing: 2f,
            kerning: [new Vector3(0, 8, 0)],
            defaultCharacter: 'A');

        Vector2 size = font.MeasureString("Z");

        Assert.Equal(8f, size.X);
        Assert.Equal(12f, size.Y);
    }

    [Fact]
    public void Constructor_MismatchedArrayLengths_Throws()
    {
        var texture = new Texture2D(nativeHandleValue: 0);

        Assert.Throws<ArgumentException>(() => new SpriteFont(
            texture,
            glyphBounds: [new Rectangle(0, 0, 10, 10)],
            cropping: [new Rectangle(0, 0, 10, 10)],
            characters: ['A', 'B'],
            lineSpacing: 12,
            spacing: 0f,
            kerning: [new Vector3(0, 8, 0)],
            defaultCharacter: null));
    }
}
