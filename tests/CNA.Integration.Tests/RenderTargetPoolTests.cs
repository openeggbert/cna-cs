using CNA.Graphics;
using CNA.Graphics.Experimental;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D3's first vertical slice: CNA's engine-layer render target pool.
///
/// The pool is the object a post-process chain is built on, and it is the smallest piece of the
/// engine layer that performs a real operation with an ownership contract worth testing: the pool
/// owns its targets, an acquired target is a borrowed view, and the pool refuses to reset while any
/// view is outstanding.
///
/// <b>Availability is asked, not inferred.</b> Every engine-layer route resolves in every build --
/// a build without the engine layer answers <c>NOT_SUPPORTED</c> at call time -- so a test that
/// concluded "the symbol is there, therefore it works" would be measuring nothing.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class RenderTargetPoolTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    [NativeFact]
    public void Pool_AcquiresReusesAndRefusesResetWhileBorrowed()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPoolRefusedWithoutTheEngineLayer(device);
                return;
            }

            output.WriteLine($"engine layer version {GraphicsDevice.CnaEngineLayerVersion()}");

            using var pool = new RenderTargetPool(device);
            Assert.Equal(0, pool.TargetCount);

            using (PooledRenderTarget first = pool.Acquire(64, 64, SurfaceFormat.Color, DepthFormat.None))
            {
                Assert.Equal(64, first.Target.Width);
                Assert.Equal(64, first.Target.Height);
                Assert.Equal(1, pool.TargetCount);
                Assert.True(pool.EstimatedBytes > 0, "a pool holding a 64x64 target must estimate some cost");

                // The pool must refuse while the view is outstanding. That refusal is what makes the
                // borrow safe rather than merely documented, so it is asserted rather than assumed.
                CNA.CnaException refused = Assert.Throws<CNA.CnaException>(pool.Reset);
                output.WriteLine($"reset while borrowed: {refused.NativeResult}");

                // A second slot of the same shape is a second target, not the same one handed out
                // twice -- which is the whole reason the slot parameter exists.
                using PooledRenderTarget second = pool.Acquire(64, 64, SurfaceFormat.Color, DepthFormat.None, slot: 1);
                Assert.NotSame(first.Target, second.Target);
                Assert.Equal(2, pool.TargetCount);
            }

            // Released, so the pool will now reset.
            pool.Reset();
            Assert.Equal(0, pool.TargetCount);
            output.WriteLine("reset after release: pool is empty");
        });
    }

    /// <summary>
    /// A borrowed target is usable as a render target, not merely a handle with dimensions.
    ///
    /// Binding it and reading the result back is what separates "the pool returned something" from
    /// "the pool returned a target this renderer can draw into".
    /// </summary>
    [NativeFact]
    public void PooledTarget_CanBeBoundAndDrawnInto()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPoolRefusedWithoutTheEngineLayer(device);
                return;
            }

            using var pool = new RenderTargetPool(device);
            using PooledRenderTarget borrowed = pool.Acquire(16, 16, SurfaceFormat.Color, DepthFormat.None);

            device.SetRenderTarget(borrowed.Target);
            try
            {
                device.Clear(new Color(0, 128, 255, 255));
            }
            finally
            {
                device.SetRenderTarget(null);
            }

            var pixels = new Color[16 * 16];
            borrowed.Target.GetData(pixels);

            output.WriteLine($"cleared pooled target reads back {pixels[0].R},{pixels[0].G},{pixels[0].B}");
            Assert.All(pixels, pixel =>
            {
                Assert.True(pixel.B > pixel.R, "the clear colour is blue-dominant and must read back that way");
            });
        });
    }

    /// <summary>
    /// What a build without the engine layer must do, asserted rather than skipped.
    ///
    /// This is the branch D3's design was built around and could not exercise: every engine-layer
    /// symbol resolves in every CNA build, so "the symbol is there" proves nothing, and the
    /// OPENGLES3 build this suite normally runs against *has* the layer. A HEADLESS build of the
    /// same revision does not, and answers <c>NOT_SUPPORTED</c> with "This CNA build does not
    /// contain the extended graphics layer" -- so the pool refuses to construct instead of handing
    /// back an object whose every later call would fail.
    ///
    /// The version is asserted too, because the header's rule is that zero means absent and the two
    /// answers come from different routes: a binding that read the wrong one would agree with itself
    /// on the build where the layer is present and disagree on this one.
    /// </summary>
    private void AssertPoolRefusedWithoutTheEngineLayer(GraphicsDevice device)
    {
        Assert.Equal(0, GraphicsDevice.CnaEngineLayerVersion());

        CnaNativeProbe.AssertRefusedAsNotSupported(
            "constructing a RenderTargetPool", () => new RenderTargetPool(device).Dispose(), output);
    }
}
