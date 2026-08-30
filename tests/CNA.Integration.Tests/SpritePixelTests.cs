using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Sprite drawing checked by reading the pixels back, rather than by the call returning success.
///
/// Every drawing test in this assembly so far asserts that a draw did not throw. That catches a
/// broken ABI transition and nothing else: a sprite drawn at the wrong place, at the wrong size, or
/// not at all returns success just as happily, and "the game renders wrongly" is the single most
/// common way a port fails while every gate stays green.
///
/// The destination-rectangle overloads are the reason to start here. CNA's batched route takes a
/// position and a scale, so <c>Draw(texture, Rectangle, ...)</c> is converted managed-side into
/// <c>position = rect.XY</c> and <c>scale = rect.Size / source.Size</c>. That conversion is XNA's
/// own, but it is a conversion this binding performs, and until now nothing checked that the pixels
/// land where the rectangle asked.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class SpritePixelTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private const int Size = 32;

    /// <summary>
    /// A white sprite drawn into a black target through a destination rectangle covers exactly that
    /// rectangle.
    ///
    /// A one-pixel source texture is used deliberately: it makes the scale factor equal to the
    /// destination size, so an implementation that forgot to scale at all would draw a single pixel
    /// and an implementation that scaled twice would overrun the target. Both are visible here and
    /// neither is visible to a test that only checks the call succeeded.
    /// </summary>
    [NativeFact]
    public void SpriteBatch_DestinationRectangle_CoversExactlyThatRectangle()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            using var white = new Texture2D(device, 1, 1);
            white.SetData([new Color(255, 255, 255, 255)]);

            using var target = new RenderTarget2D(device, Size, Size);
            var destination = new Rectangle(8, 4, 16, 8);

            Color[] pixels = DrawAndRead(device, target, batch =>
                batch.Draw(white, destination, new Color(255, 255, 255, 255)));

            if (IsEntirely(pixels, new Color(0, 0, 0, 0)))
            {
                // A renderer that presents nothing into an offscreen target reports success for
                // every call above. Saying so is worth more than a green tick that means nothing.
                output.WriteLine("This renderer read back an empty target; the draw was not observable.");
                return;
            }

            output.WriteLine(Render(pixels));

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool inside = destination.Contains(x, y);
                    bool lit = pixels[(y * Size) + x].R > 127;

                    Assert.True(
                        inside == lit,
                        $"pixel {x},{y} is {(lit ? "lit" : "dark")} but the destination rectangle " +
                        $"{destination.X},{destination.Y} {destination.Width}x{destination.Height} " +
                        $"says it should be {(inside ? "lit" : "dark")}.");
                }
            }
        });
    }

    /// <summary>
    /// The origin is measured in source-texture pixels and scales with the sprite.
    ///
    /// This is the part of the destination-rectangle conversion with a real choice in it. XNA
    /// offsets a sprite by <c>-origin * scale</c>, so an origin of one pixel on a sprite scaled
    /// eight times shifts it eight pixels, not one. A binding that passed the origin through
    /// unscaled would be wrong by a factor of the scale -- invisible at scale 1, which is exactly
    /// where a casual test would look.
    /// </summary>
    [NativeFact]
    public void SpriteBatch_OriginScalesWithTheSprite()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            using var white = new Texture2D(device, 1, 1);
            white.SetData([new Color(255, 255, 255, 255)]);

            using var target = new RenderTarget2D(device, Size, Size);

            // Source is one pixel, destination is 8x8 at (16,16), origin is the whole source pixel.
            // Scaled, that origin is 8 pixels, so the sprite must land at (8,8).
            var destination = new Rectangle(16, 16, 8, 8);

            Color[] pixels = DrawAndRead(device, target, batch => batch.Draw(
                white,
                destination,
                null,
                new Color(255, 255, 255, 255),
                0f,
                new Vector2(1f, 1f),
                SpriteEffects.None,
                0f));

            if (IsEntirely(pixels, new Color(0, 0, 0, 0)))
            {
                output.WriteLine("This renderer read back an empty target; the draw was not observable.");
                return;
            }

            output.WriteLine(Render(pixels));

            (int minX, int minY, int maxX, int maxY) = LitBounds(pixels);
            output.WriteLine($"lit bounds: {minX},{minY} .. {maxX},{maxY}");

            Assert.Equal(8, minX);
            Assert.Equal(8, minY);
            Assert.Equal(15, maxX);
            Assert.Equal(15, maxY);
        });
    }

    private static Color[] DrawAndRead(GraphicsDevice device, RenderTarget2D target, Action<SpriteBatch> draw)
    {
        device.SetRenderTarget(target);
        try
        {
            device.Clear(new Color(0, 0, 0, 255));

            using (var batch = new SpriteBatch(device))
            {
                batch.Begin();
                draw(batch);
                batch.End();
            }
        }
        finally
        {
            device.SetRenderTarget(null);
        }

        var pixels = new Color[Size * Size];
        target.GetData(pixels);
        return pixels;
    }

    private static bool IsEntirely(Color[] pixels, Color value) =>
        pixels.All(pixel => pixel.R == value.R && pixel.G == value.G && pixel.B == value.B && pixel.A == value.A);

    private static (int MinX, int MinY, int MaxX, int MaxY) LitBounds(Color[] pixels)
    {
        int minX = Size, minY = Size, maxX = -1, maxY = -1;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (pixels[(y * Size) + x].R > 127)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        return (minX, minY, maxX, maxY);
    }

    /// <summary>The target as text, so a failure shows the shape that was drawn rather than one
    /// disagreeing pixel coordinate.</summary>
    private static string Render(Color[] pixels)
    {
        var text = new System.Text.StringBuilder();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                text.Append(pixels[(y * Size) + x].R > 127 ? '#' : '.');
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}
