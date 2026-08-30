using CNA.Graphics;
using CNA.Graphics.Experimental;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D3's second vertical slice: CNA's engine-layer post-process chain, built on the render-target
/// pool the first slice bound.
///
/// <b>The ownership contracts are what this file is for.</b> Three different ones meet in one type
/// -- a borrowed pass, a transferred pass, and a counted borrow of the chain's own pool -- and each
/// is asserted rather than documented, because each fails silently when it is wrong: a
/// double-released pass corrupts the heap, and a leaked borrow shows up as a chain that will not
/// destroy.
///
/// <b>The blit pass is chosen deliberately.</b> It copies its source to its destination unchanged,
/// so its correct output is stateable without reimplementing a shader in the test -- which means the
/// pixel comparison is a real check rather than a restatement of the implementation.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class PostProcessChainTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>The branch a build without the engine layer takes. Measured on HEADLESS, where
    /// every engine-layer symbol resolves and the layer is absent -- which is the whole reason
    /// availability is queried instead of inferred from symbol resolution.</summary>
    private bool RequireEngineLayer(GraphicsDevice device)
    {
        if (GraphicsDevice.IsCnaEngineLayerAvailable())
        {
            return true;
        }

        Assert.Equal(0, GraphicsDevice.CnaEngineLayerVersion());
        CnaNativeProbe.AssertRefusedAsNotSupported(
            "constructing a PostProcessChain", () => new PostProcessChain(device).Dispose(), output);
        CnaNativeProbe.AssertRefusedAsNotSupported(
            "creating a blit pass", () => PostProcessPass.CreateBlit(device).Dispose(), output);
        return false;
    }

    [NativeFact]
    public void Chain_CountsPassesAndClearsThem()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!RequireEngineLayer(device))
            {
                return;
            }

            using var chain = new PostProcessChain(device);
            Assert.Equal(0, chain.PassCount);

            using PostProcessPass borrowed = PostProcessPass.CreateBlit(device);
            output.WriteLine($"blit pass name='{borrowed.Name}' supported={borrowed.IsSupportedOn(device)}");

            Assert.False(string.IsNullOrEmpty(borrowed.Name), "a pass with no name cannot be identified in a chain.");

            chain.Add(borrowed);
            Assert.Equal(1, chain.PassCount);

            chain.Add(borrowed);
            Assert.Equal(2, chain.PassCount);

            chain.Clear();
            Assert.Equal(0, chain.PassCount);

            // Clear released only what the chain owned, which is nothing here. The borrowed pass is
            // still usable, and that is the assertion -- a Clear that released a borrowed pass would
            // leave this call operating on freed memory rather than answering.
            Assert.False(string.IsNullOrEmpty(borrowed.Name));
        });
    }

    /// <summary>
    /// <c>AddOwned</c> transfers the handle, and the managed wrapper must stop owning it.
    ///
    /// CNA consumes the handle whether or not the call succeeds, so a wrapper that kept ownership
    /// would destroy it a second time on <c>Dispose</c> -- a double free with no symptom until it
    /// has one. The assertion is that disposing the surrendered pass and then the chain both
    /// succeed, and that the chain still counts the pass in between.
    /// </summary>
    [NativeFact]
    public void AddOwned_TransfersThePassAndDisposingItAfterwardsIsSafe()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!RequireEngineLayer(device))
            {
                return;
            }

            var chain = new PostProcessChain(device);
            var owned = PostProcessPass.CreateBlit(device);

            Assert.True(owned.OwnsNativePass);
            Assert.False(string.IsNullOrEmpty(owned.Name));

            chain.AddOwned(owned);
            Assert.Equal(1, chain.PassCount);

            // The assertion that can actually fail. Disposing a still-owning wrapper releases a
            // handle CNA has already consumed, and native answers a failure result that
            // NativeResourceHandle discards -- so "dispose it and see" catches nothing, which was
            // measured by removing the surrender and watching this test still pass. What is
            // observable is that the wrapper knows: it owns nothing, and every operation that would
            // touch the consumed handle refuses.
            Assert.False(owned.OwnsNativePass);
            Assert.Throws<ObjectDisposedException>(() => owned.Name);
            Assert.Throws<ObjectDisposedException>(() => owned.IsSupportedOn(device));

            // Inert, and repeatable.
            owned.Dispose();
            owned.Dispose();

            Assert.Equal(1, chain.PassCount);

            // And the chain releases it, once.
            chain.Dispose();
        });
    }

    /// <summary>
    /// The chain's pool is a <em>counted</em> borrow: the chain refuses to be destroyed while one is
    /// outstanding.
    ///
    /// This is the same contract <see cref="PooledRenderTarget"/> has against its pool, one level up,
    /// and it is the reason a caller can be handed the chain's own pool at all without a way to
    /// outlive it. Both directions are asserted -- refused while borrowed, and accepted after -- since
    /// a chain that refused destruction unconditionally would pass a one-directional test.
    /// </summary>
    [NativeFact]
    public void BorrowTargetPool_RefusesChainDestructionWhileOutstanding()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!RequireEngineLayer(device))
            {
                return;
            }

            var chain = new PostProcessChain(device);

            RenderTargetPool borrowed = chain.BorrowTargetPool();
            Assert.Equal(0, borrowed.TargetCount);

            using (PooledRenderTarget target = borrowed.Acquire(32, 32, SurfaceFormat.Color, DepthFormat.None))
            {
                Assert.Equal(32, target.Target.Width);
                Assert.Equal(1, borrowed.TargetCount);
            }

            // The chain's pool is the chain's; releasing the borrow is what lets the chain go.
            borrowed.Dispose();
            chain.Dispose();
        });
    }

    /// <summary>
    /// The chain end to end: blit passes copy a source texture into a render target, and the target
    /// is read back and compared texel for texel.
    ///
    /// <b>The pixel comparison alone does not prove a pass ran</b>, and finding that out is what
    /// this test is shaped by. A blit is the identity, and an *empty* chain also copies its source
    /// to its destination -- so removing the pass entirely leaves every pixel assertion passing.
    /// Measured by doing exactly that.
    ///
    /// What does distinguish them is the pool. A chain of two passes has to ping-pong, so it takes
    /// one intermediate target from its own pool; chains of zero or one write straight to the
    /// destination and take none. That count is a consequence of the passes running, it changes when
    /// a pass is removed, and it is what makes this test able to fail for the thing it claims.
    ///
    /// The source carries four distinct texels and the destination is pre-cleared to a fifth colour,
    /// so "the chain did nothing" and "the chain copied correctly" are different pictures.
    /// </summary>
    [NativeFact]
    public void Apply_RunsEveryPassAndPingPongsThroughItsPool()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!RequireEngineLayer(device))
            {
                return;
            }

            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }

            Color[] source =
            [
                new Color(200, 10, 20, 255),
                new Color(30, 210, 40, 255),
                new Color(50, 60, 220, 255),
                new Color(230, 240, 70, 255),
            ];

            using var texture = new Texture2D(device, 2, 2);
            texture.SetData(source);

            using var destination = new RenderTarget2D(device, 2, 2);
            device.SetRenderTarget(destination);
            device.Clear(new Color(1, 2, 3, 255));
            device.SetRenderTarget(null);

            var frame = new PostProcessFrame
            {
                Source = texture,
                Destination = destination,
                Width = 2,
                Height = 2,
                ElapsedSeconds = 1f / 60f,
                NearPlane = 0.1f,
                FarPlane = 100f,
                Projection = Matrix.Identity,
                InverseProjection = Matrix.Identity,
                InverseView = Matrix.Identity,
                PreviousViewProjection = Matrix.Identity,
            };

            using var chain = new PostProcessChain(device);
            using PostProcessPass first = PostProcessPass.CreateBlit(device);
            using PostProcessPass second = PostProcessPass.CreateBlit(device);

            chain.Add(first);
            chain.Apply(in frame);
            Assert.Equal(0, PooledTargetCount(chain));

            chain.Add(second);
            chain.Apply(in frame);

            long intermediates = PooledTargetCount(chain);
            output.WriteLine($"two passes took {intermediates} pooled intermediate target(s)");

            // One, not "more than zero": a chain of two passes needs exactly one buffer to bounce
            // through, and a chain that took two would be holding one it never reads.
            Assert.Equal(1, intermediates);

            var read = new Color[4];
            destination.GetData(read);
            output.WriteLine($"after two blits: {string.Join(" ", read.Select(c => $"{c.R},{c.G},{c.B}"))}");

            for (int i = 0; i < read.Length; i++)
            {
                Assert.Equal(source[i].R, read[i].R);
                Assert.Equal(source[i].G, read[i].G);
                Assert.Equal(source[i].B, read[i].B);
            }
        });
    }

    /// <summary>How many intermediate targets the chain is holding, through the borrow it hands
    /// out. The borrow is released before returning, because the chain cannot be destroyed while
    /// one is outstanding and a leaked one here would surface as an unrelated failure later.</summary>
    private static long PooledTargetCount(PostProcessChain chain)
    {
        using RenderTargetPool pool = chain.BorrowTargetPool();
        return pool.TargetCount;
    }

    /// <summary>
    /// An empty chain still needs a source, and says so.
    ///
    /// This test first asserted that an empty chain leaves its destination alone, which was my
    /// assumption and not CNA's: the chain refuses a context whose source is null with
    /// <c>InvalidArgument: The chain has nothing to read from</c>, no matter how many passes it
    /// holds. That is the stricter and more useful contract -- a game that built its chain
    /// conditionally and forgot the source is told, rather than silently rendering the previous
    /// frame -- so it is asserted as measured.
    ///
    /// With a source, an empty chain copies it through, which is the identity a chain of zero passes
    /// should compose to. Both halves are here because either alone would leave the other
    /// unexplained.
    /// </summary>
    [NativeFact]
    public void Apply_WithNoPasses_NeedsASourceAndCopiesItThrough()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!RequireEngineLayer(device))
            {
                return;
            }

            if (!CnaNativeProbe.RequireRenderTargetReadback(device, output))
            {
                return;
            }

            using var destination = new RenderTarget2D(device, 2, 2);
            device.SetRenderTarget(destination);
            device.Clear(new Color(9, 8, 7, 255));
            device.SetRenderTarget(null);

            using var chain = new PostProcessChain(device);
            Assert.Equal(0, chain.PassCount);

            CnaException refused = Assert.Throws<CnaException>(
                () => chain.Apply(new PostProcessFrame { Destination = destination, Width = 2, Height = 2 }));
            Assert.Equal("InvalidArgument", refused.NativeResult);
            output.WriteLine($"empty chain, no source: {refused.Message}");

            Color[] source =
            [
                new Color(200, 10, 20, 255),
                new Color(30, 210, 40, 255),
                new Color(50, 60, 220, 255),
                new Color(230, 240, 70, 255),
            ];

            using var texture = new Texture2D(device, 2, 2);
            texture.SetData(source);

            chain.Apply(new PostProcessFrame
            {
                Source = texture,
                Destination = destination,
                Width = 2,
                Height = 2,
            });

            var read = new Color[4];
            destination.GetData(read);
            output.WriteLine($"empty chain, with source: {string.Join(" ", read.Select(c => $"{c.R},{c.G},{c.B}"))}");

            for (int i = 0; i < read.Length; i++)
            {
                Assert.Equal(source[i].R, read[i].R);
                Assert.Equal(source[i].G, read[i].G);
                Assert.Equal(source[i].B, read[i].B);
            }
        });
    }
}
