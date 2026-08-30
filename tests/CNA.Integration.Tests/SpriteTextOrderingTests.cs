using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Sprites and strings now leave through two different native routes, so the batch replays them
/// interleaved to keep the order the game issued them. This is where that is checked.
///
/// The failure this guards is specific and quiet: submitting every sprite and then every string is
/// one native call fewer, always draws, and puts a HUD underneath the scene it labels. No return
/// code reports it, which is why these read pixels.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class SpriteTextOrderingTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private const int Size = 32;

    private static readonly Color Red = new(255, 0, 0, 255);
    private static readonly Color Green = new(0, 255, 0, 255);
    private static readonly Color Blue = new(0, 0, 255, 255);
    private static readonly Color White = new(255, 255, 255, 255);

    /// <summary>
    /// A sprite issued after a string covers it, and a sprite issued before it does not.
    ///
    /// Both directions are asserted because only the pair pins the order down. A batch that always
    /// drew text last would pass the second case, and one that always drew text first would pass the
    /// first.
    /// </summary>
    [NativeFact]
    public void TextAndSprites_ReachTheRendererInIssueOrder()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }

            using var fixtureAssets = new TextFixture(device);

            // Text first, then a sprite over it: the sprite wins.
            Color[] spriteLast = Render(device, batch =>
            {
                batch.DrawString(fixtureAssets.Font, "AB", new Vector2(8f, 8f), White,
                    0f, Vector2.Zero, new Vector2(4f, 4f), SpriteEffects.None, 0f);
                batch.Draw(fixtureAssets.Blue, new Rectangle(8, 8, 8, 4), Blue);
            });

            // Sprite first, then text over it: the text wins.
            Color[] textLast = Render(device, batch =>
            {
                batch.Draw(fixtureAssets.Blue, new Rectangle(8, 8, 8, 4), Blue);
                batch.DrawString(fixtureAssets.Font, "AB", new Vector2(8f, 8f), White,
                    0f, Vector2.Zero, new Vector2(4f, 4f), SpriteEffects.None, 0f);
            });

            output.WriteLine("sprite issued last:\n" + Describe(spriteLast));
            output.WriteLine("text issued last:\n" + Describe(textLast));

            // (9,9) is inside both the glyph 'A' block and the sprite, so whichever came last owns it.
            AssertPixelIs(spriteLast, 9, 9, Blue, "the sprite was issued after the string");
            AssertPixelIs(textLast, 9, 9, Red, "the string was issued after the sprite");
        });
    }

    /// <summary>
    /// The per-glyph fallback draws the same pixels as the native text route.
    ///
    /// A renderer is allowed to refuse <c>cna_sprite_batch_draw_string</c>, and every renderer could
    /// draw text before this change, so the fallback must be equivalent rather than merely present.
    /// This renderer accepts the native route, so the fallback would otherwise never run here at
    /// all.
    /// </summary>
    [NativeFact]
    public void GlyphQuadFallback_DrawsWhatTheNativeRouteDraws()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }

            using var fixtureAssets = new TextFixture(device);

            void Draw(SpriteBatch batch) => batch.DrawString(
                fixtureAssets.Font, "AB", new Vector2(8f, 8f), White,
                0f, Vector2.Zero, new Vector2(4f, 4f), SpriteEffects.None, 0f);

            Color[] viaNative = Render(device, Draw);

            // The glyphs really were drawn, so the comparison below cannot be between two blank
            // targets -- the mistake the A1 measurement made and had to be rewritten to avoid.
            AssertPixelIs(viaNative, 9, 9, Red, "the native text route drew the 'A' glyph");
            AssertPixelIs(viaNative, 13, 9, Green, "the native text route drew the 'B' glyph");

            Color[] viaGlyphQuads = Render(device, batch =>
            {
                batch.ForceGlyphQuadTextForTesting();
                Draw(batch);
            });

            int differing = 0;
            for (int i = 0; i < viaNative.Length; i++)
            {
                if (!Same(viaNative[i], viaGlyphQuads[i]))
                {
                    differing++;
                }
            }

            output.WriteLine($"fallback differs from the native route in {differing} of {viaNative.Length} pixels");
            Assert.Equal(0, differing);
        });
    }

    private sealed class TextFixture : IDisposable
    {
        public TextFixture(GraphicsDevice device)
        {
            Atlas = new Texture2D(device, 3, 1);
            Atlas.SetData([new Color(0, 0, 0, 255), Red, Green]);

            Blue = new Texture2D(device, 1, 1);
            Blue.SetData([SpriteTextOrderingTests.Blue]);

            Font = new SpriteFont(
                Atlas,
                glyphBounds: [new Rectangle(0, 0, 1, 1), new Rectangle(1, 0, 1, 1), new Rectangle(2, 0, 1, 1)],
                cropping: [new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1)],
                characters: [' ', 'A', 'B'],
                lineSpacing: 2,
                spacing: 0f,
                kerning: [new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f)],
                defaultCharacter: ' ');
        }

        public Texture2D Atlas { get; }

        public Texture2D Blue { get; }

        public SpriteFont Font { get; }

        public void Dispose()
        {
            Atlas.Dispose();
            Blue.Dispose();
        }
    }

    private static Color[] Render(GraphicsDevice device, Action<SpriteBatch> draw)
    {
        using var target = new RenderTarget2D(device, Size, Size);

        device.SetRenderTarget(target);
        try
        {
            device.Clear(new Color(0, 0, 0, 255));
            using var batch = new SpriteBatch(device);
            batch.Begin();
            draw(batch);
            batch.End();
        }
        finally
        {
            device.SetRenderTarget(null);
        }

        var pixels = new Color[Size * Size];
        target.GetData(pixels);
        return pixels;
    }

    /// <summary>
    /// Asserts which of the three fixture colours owns a pixel, by dominance rather than by exact
    /// value.
    ///
    /// Exactness was the first attempt and it was wrong: the glyph atlas is three texels wide and
    /// the batch samples it with LinearClamp, so a one-texel glyph drawn at scale 4 picks up its
    /// neighbours and the "red" block reads 223,0,0 rather than 255,0,0. That is correct filtering,
    /// not a misplaced glyph, and an assertion that cannot tell the two apart reports the wrong
    /// thing. What these tests are actually about is *which* sprite owns a pixel, and dominance says
    /// exactly that while staying a positive claim about what must be there.
    /// </summary>
    private static void AssertPixelIs(Color[] pixels, int x, int y, Color expected, string because)
    {
        Color actual = pixels[(y * Size) + x];

        static int Dominant(Color color) =>
            color.R >= color.G && color.R >= color.B ? 0 : color.G >= color.B ? 1 : 2;

        Assert.True(
            Dominant(actual) == Dominant(expected) && Math.Max(actual.R, Math.Max(actual.G, actual.B)) > 127,
            $"expected a pixel dominated by {expected.R},{expected.G},{expected.B} at {x},{y} " +
            $"because {because}; found {actual.R},{actual.G},{actual.B}.");
    }

    private static bool Same(Color left, Color right) =>
        left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;

    private static string Describe(Color[] pixels)
    {
        var text = new System.Text.StringBuilder();
        for (int y = 6; y < 14; y++)
        {
            for (int x = 6; x < 20; x++)
            {
                Color pixel = pixels[(y * Size) + x];
                text.Append(pixel.B > 127 ? 'b' : pixel.R > 127 ? 'A' : pixel.G > 127 ? 'B' : '.');
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}
