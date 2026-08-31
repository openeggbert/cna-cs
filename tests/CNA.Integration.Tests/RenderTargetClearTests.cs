using CNA.Graphics;
using Microsoft.Xna.Framework;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The smallest statement of a contract most of the pixel evidence in this suite silently depends
/// on: <c>Clear</c> into a bound render target must be observable when that target is read back.
///
/// It exists because three tests -- two post-process ones and the pooled-target one -- started
/// failing together against CNA ABI 0.21.0, each reporting an unrelated-looking symptom (a blit
/// that produced black, a pooled target that would not take a clear). One defect wearing three
/// costumes is hard to act on, so this test wears the defect's own face: no engine layer, no
/// sprite batch, no pool. Bind, clear, unbind, read.
///
/// <b>It is currently RED, and that is the correct state.</b> The defect is upstream's and is
/// recorded in docs/native-behavior-blockers.md; when CNA fixes it this goes green on its own and
/// the blocker row closes with evidence rather than with a reading of a commit message.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class RenderTargetClearTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>
    /// Bind, clear, unbind, read. Nothing else.
    ///
    /// The clear colour is blue-dominant and fully opaque so that the failure mode is legible: a
    /// lost clear reads back <c>0,0,0,0</c>, which no channel of the requested colour matches, and
    /// the alpha distinguishes "cleared to transparent black" from "never written".
    /// </summary>
    [NativeFact]
    public void Clear_IntoABoundRenderTarget_IsObservableOnReadback()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!CnaNativeProbe.SupportsRenderTargetReadback(device, output))
            {
                output.WriteLine(
                    $"NOT EXERCISED: renderer '{device.RendererName}' cannot read a render target back.");
                return;
            }

            using var target = new RenderTarget2D(device, 4, 4);
            device.SetRenderTarget(target);
            try
            {
                device.Clear(new Color(0, 128, 255, 255));
            }
            finally
            {
                device.SetRenderTarget(null);
            }

            var pixels = new Color[16];
            target.GetData(pixels);
            output.WriteLine(
                $"cleared 4x4 target on '{device.RendererName}' reads back " +
                $"{pixels[0].R},{pixels[0].G},{pixels[0].B},{pixels[0].A}");

            Assert.All(pixels, pixel => Assert.Equal(new Color(0, 128, 255, 255), pixel));
        });
    }

    /// <summary>
    /// The same clear, with one sprite drawn afterwards -- and this one passes.
    ///
    /// It is here because the difference between the two is the whole diagnosis. A drawn sprite
    /// lands, and the clear underneath it lands with it; the clear alone does not. So neither the
    /// render target, nor readback, nor the binding's clear arguments are at fault -- what is lost
    /// is a clear that nothing follows before the target is unbound. Deleting this test would leave
    /// the one above looking like "render targets are broken", which is measurably not the case.
    /// </summary>
    [NativeFact]
    public void Clear_FollowedByADraw_IsObservableForBoth()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!CnaNativeProbe.SupportsRenderTargetReadback(device, output))
            {
                output.WriteLine(
                    $"NOT EXERCISED: renderer '{device.RendererName}' cannot read a render target back.");
                return;
            }

            using var source = new Texture2D(device, 1, 1);
            source.SetData([new Color(255, 0, 0, 255)]);

            using var target = new RenderTarget2D(device, 8, 8);
            device.SetRenderTarget(target);
            try
            {
                device.Clear(new Color(0, 128, 255, 255));
                using var batch = new SpriteBatch(device);
                batch.Begin();
                batch.Draw(source, new Rectangle(0, 0, 4, 4), Color.White);
                batch.End();
            }
            finally
            {
                device.SetRenderTarget(null);
            }

            var pixels = new Color[64];
            target.GetData(pixels);

            // 0,0 is under the sprite; 7,0 is outside it and shows only the clear.
            output.WriteLine(
                $"drawn={pixels[0].R},{pixels[0].G},{pixels[0].B} " +
                $"cleared={pixels[7].R},{pixels[7].G},{pixels[7].B}");

            Assert.Equal(new Color(255, 0, 0, 255), pixels[0]);
            Assert.Equal(new Color(0, 128, 255, 255), pixels[7]);
        });
    }
}
