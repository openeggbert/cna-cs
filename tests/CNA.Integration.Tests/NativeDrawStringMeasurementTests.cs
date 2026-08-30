using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// plan.md A1: does <c>cna_sprite_batch_draw_string</c> put glyphs where the per-glyph quad path
/// puts them?
///
/// The plan says to measure before adopting, and to record the measurement either way. Until
/// <c>SpritePixelTests</c> there was no way to measure it at all -- both routes return success, and
/// success says nothing about where a glyph landed. This renders the same string through both and
/// compares the target pixel for pixel.
///
/// The font is deliberately synthetic: one texel per glyph, distinct colours, drawn at scale 4. A
/// real font would make a one-pixel disagreement arguable; here every glyph is a solid 4x4 block, so
/// a difference in placement, ordering or tint is visible as a block in the wrong place rather than
/// as a handful of edge texels.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class NativeDrawStringMeasurementTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private const int Size = 32;
    private const float Scale = 4f;

    [NativeFact]
    public void NativeDrawString_PlacesGlyphsWhereThePerGlyphPathDoes()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }


            using var atlas = new Texture2D(device, 3, 1);
            atlas.SetData([
                new Color(0, 0, 0, 255),        // space
                new Color(255, 0, 0, 255),      // A
                new Color(0, 255, 0, 255),      // B
            ]);

            var font = new SpriteFont(
                atlas,
                glyphBounds: [new Rectangle(0, 0, 1, 1), new Rectangle(1, 0, 1, 1), new Rectangle(2, 0, 1, 1)],
                cropping: [new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1)],
                characters: [' ', 'A', 'B'],
                lineSpacing: 2,
                spacing: 0f,
                kerning: [new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f)],
                defaultCharacter: ' ');

            Color[] managed = RenderWith(device, batch =>
            {
                // Force the per-glyph route. Since adoption, DrawString *is* the native route, so
                // without this both arms would draw the same way and the comparison would report
                // perfect agreement while measuring nothing -- the same vacuity this test was
                // already rewritten once to avoid.
                batch.ForceGlyphQuadTextForTesting();
                batch.DrawString(
                    font, "AB", new Vector2(8f, 8f), new Color(255, 255, 255, 255),
                    0f, Vector2.Zero, new Vector2(Scale, Scale), SpriteEffects.None, 0f);
            });

            // Before comparing anything, prove the reference render is real. Comparing two blank
            // targets agrees perfectly and means nothing, and the first version of this test could
            // have done exactly that: its "is it empty" check looked for a zero alpha, while a
            // target cleared to opaque black has alpha 255. The check now names the pixels the
            // per-glyph path is supposed to produce, so the comparison below cannot be vacuous.
            AssertGlyphsLandedWhereExpected(managed);

            if (font.NativeFontHandleValue == 0)
            {
                // A build without the native SpriteFont resource answers here. That is a measurement
                // result -- "not available" -- not a defect in this binding.
                output.WriteLine("A1 MEASUREMENT: no native font could be built on this renderer.");
                return;
            }

            Color[] native = RenderWith(device, batch => batch.DrawString(
                font, "AB", new Vector2(8f, 8f), new Color(255, 255, 255, 255),
                0f, Vector2.Zero, new Vector2(Scale, Scale), SpriteEffects.None, 0f));

            int differing = 0;
            for (int i = 0; i < managed.Length; i++)
            {
                if (!Same(managed[i], native[i]))
                {
                    differing++;
                }
            }

            output.WriteLine($"A1 MEASUREMENT: {differing} of {managed.Length} pixels differ.");
            output.WriteLine("per-glyph path:\n" + Render(managed));
            output.WriteLine("native draw_string:\n" + Render(native));

            Assert.Equal(0, differing);
        });
    }

    /// <summary>
    /// What adopting the native route would actually cost, measured rather than assumed.
    ///
    /// plan.md recorded the win as "one native call per string instead of one per glyph". That is
    /// wrong, and reading <c>DrawEx</c> settles it: every <c>Draw</c> and every glyph of every
    /// <c>DrawString</c> appends to <c>_commandBuffer</c>, and the whole batch leaves through a
    /// single <c>cna_sprite_batch_submit_scaled_many</c> at <c>End</c>. Today's cost is one native
    /// transition per *batch*, whatever the glyph count. The native text route costs one per
    /// *string*, so adopting it trades fewer managed glyph placements for more ABI crossings.
    ///
    /// Which of those wins is not something to reason about, so this measures it on a text-heavy
    /// frame. The comparison is fair for text-only work: <c>FlushCommandBuffer</c> returns
    /// immediately on an empty buffer, so the native path here really does make one call per string
    /// and no more, which is the best an adopted version could do.
    ///
    /// This reports and does not assert a threshold. A timing assertion on shared CI hardware fails
    /// for reasons that have nothing to do with the code, and the decision this informs is a design
    /// decision made once, not a regression to guard every run.
    /// </summary>
    [NativeFact]
    public void NativeDrawString_CostComparedToTheBufferedPath()
    {
        const int StringsPerFrame = 64;
        const int Frames = 60;

        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }


            using var atlas = new Texture2D(device, 3, 1);
            atlas.SetData([
                new Color(0, 0, 0, 255), new Color(255, 0, 0, 255), new Color(0, 255, 0, 255),
            ]);

            SpriteFont font = BuildFont(atlas);
            string text = string.Concat(Enumerable.Repeat("AB", 12));   // 24 glyphs

            using var target = new RenderTarget2D(device, Size, Size);

            double managed = TimeFrames(device, target, Frames, batch =>
            {
                batch.ForceGlyphQuadTextForTesting();
                for (int i = 0; i < StringsPerFrame; i++)
                {
                    batch.DrawString(font, text, new Vector2(0f, i % Size), new Color(255, 255, 255, 255));
                }
            });

            double native = TimeFrames(device, target, Frames, batch =>
            {
                for (int i = 0; i < StringsPerFrame; i++)
                {
                    batch.DrawString(font, text, new Vector2(0f, i % Size), new Color(255, 255, 255, 255));
                }
            });

            output.WriteLine(
                $"A1 COST: {StringsPerFrame} strings x 24 glyphs, {Frames} frames. " +
                $"buffered per-glyph {managed:F2} ms/frame (1 native call per frame); " +
                $"native draw_string {native:F2} ms/frame ({StringsPerFrame} native calls per frame); " +
                $"ratio {(managed <= 0 ? 0 : native / managed):F2}x");
        });
    }

    private static double TimeFrames(
        GraphicsDevice device, RenderTarget2D target, int frames, Action<SpriteBatch> draw)
    {
        using var batch = new SpriteBatch(device);

        // One untimed frame so shader/pipeline setup and any first-call allocation are not counted
        // as the cost of the route under test.
        RunFrame(device, target, batch, draw);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            RunFrame(device, target, batch, draw);
        }

        clock.Stop();
        return clock.Elapsed.TotalMilliseconds / frames;
    }

    private static void RunFrame(
        GraphicsDevice device, RenderTarget2D target, SpriteBatch batch, Action<SpriteBatch> draw)
    {
        device.SetRenderTarget(target);
        try
        {
            device.Clear(new Color(0, 0, 0, 255));
            batch.Begin();
            draw(batch);
            batch.End();
        }
        finally
        {
            device.SetRenderTarget(null);
        }
    }

    private static SpriteFont BuildFont(Texture2D atlas) => new(
        atlas,
        glyphBounds: [new Rectangle(0, 0, 1, 1), new Rectangle(1, 0, 1, 1), new Rectangle(2, 0, 1, 1)],
        cropping: [new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1)],
        characters: [' ', 'A', 'B'],
        lineSpacing: 2,
        spacing: 0f,
        kerning: [new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 0f)],
        defaultCharacter: ' ');

    private static Color[] RenderWith(GraphicsDevice device, Action<SpriteBatch> draw)
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

    private static bool Same(Color left, Color right) =>
        left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;

    /// <summary>
    /// The reference render really drew the string: a red 4x4 block for 'A' at (8,8) and a green one
    /// for 'B' immediately after it, which is where one-texel glyphs of unit kerning drawn at
    /// position (8,8) and scale 4 have to land.
    ///
    /// This is the guard that stops the comparison being between two blank targets.
    /// </summary>
    private static void AssertGlyphsLandedWhereExpected(Color[] pixels)
    {
        for (int y = 8; y < 12; y++)
        {
            for (int x = 8; x < 12; x++)
            {
                Color pixel = pixels[(y * Size) + x];
                Assert.True(
                    pixel.R > 127 && pixel.G < 128,
                    $"expected the red 'A' block at {x},{y}, found {pixel.R},{pixel.G},{pixel.B}.");
            }

            for (int x = 12; x < 16; x++)
            {
                Color pixel = pixels[(y * Size) + x];
                Assert.True(
                    pixel.G > 127 && pixel.R < 128,
                    $"expected the green 'B' block at {x},{y}, found {pixel.R},{pixel.G},{pixel.B}.");
            }
        }
    }

    /// <summary>The target as text, one character per glyph colour, so a disagreement shows as a
    /// shape rather than as a pixel index.</summary>
    private static string Render(Color[] pixels)
    {
        var text = new System.Text.StringBuilder();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Color pixel = pixels[(y * Size) + x];
                text.Append(pixel.R > 127 ? 'A' : pixel.G > 127 ? 'B' : '.');
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}
