using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The dimensions every sprite draw resolves against.
///
/// <c>SpriteBatch.Draw</c> expands a null source rectangle to the whole texture, so it needs a
/// texture's extents on the hot path. It used to fetch them with a <c>cna_texture2d_get_info</c>
/// transition <em>per sprite per frame</em> -- twice, for the destination-rectangle overloads --
/// to re-read a number fixed when the texture was created. They are now read once and remembered
/// (<c>Texture.CachedDimensions</c>).
///
/// These tests exist because a cache is only as good as the answer it caches, and because the
/// remembering moved the render-target case onto a different native call: a
/// <see cref="RenderTarget2D"/> is a separate native resource type, and its extents now come from
/// <c>cna_render_target_get_info</c> rather than from the texture call the sprite path used to
/// make on every handle regardless of kind. Nothing covered a render target reaching
/// <c>SpriteBatch</c> as a <em>source</em> before, which is exactly why it is pinned here.
///
/// No pixels are asserted: a renderer that cannot read a colour attachment back is a supported
/// answer (see <c>RenderTargetPoolTests</c>), and these claims are about the dimensions the sprite
/// path resolves, not about what the renderer draws.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class TextureDimensionCacheTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    [NativeFact]
    public void Texture2D_ReportsItsCreatedDimensions_AndKeepsReportingThem()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 37, 11);

            Assert.Equal(37, texture.Width);
            Assert.Equal(11, texture.Height);

            // Reading again must not drift: the second answer comes from the cache, and a cache
            // that disagreed with the first read would be worse than no cache at all.
            Assert.Equal(37, texture.Width);
            Assert.Equal(11, texture.Height);

            // SetData rewrites texels, not extents.
            texture.SetData(new Color[37 * 11]);
            Assert.Equal(37, texture.Width);
            Assert.Equal(11, texture.Height);

            output.WriteLine($"texture reports {texture.Width}x{texture.Height} across repeated reads and a SetData");
        });
    }

    [NativeFact]
    public void RenderTarget2D_ReportsItsOwnDimensions_NotATextureCallsAnswer()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var target = new RenderTarget2D(device, 24, 18);

            Assert.Equal(24, target.Width);
            Assert.Equal(18, target.Height);
            Assert.Equal(24, target.Width);
            Assert.Equal(18, target.Height);

            output.WriteLine($"render target reports {target.Width}x{target.Height}");
        });
    }

    /// <summary>
    /// A render target drawn as a sprite source. This is the route the cache changed: resolving a
    /// null source rectangle for a render-target handle now asks the render-target info call.
    /// Drawing without throwing is the claim -- if that call rejected the handle, this would fail
    /// with a <see cref="CnaException"/> rather than draw.
    /// </summary>
    [NativeFact]
    public void RenderTarget2D_CanBeDrawnAsASpriteSource()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var target = new RenderTarget2D(device, 24, 18);
            using var batch = new SpriteBatch(device);

            batch.Begin();
            batch.Draw(target, Vector2.Zero, Color.White);
            batch.Draw(target, new Rectangle(0, 0, 48, 36), Color.White);
            batch.End();

            output.WriteLine("a render target resolved its own extents on both sprite routes");
        });
    }
}
