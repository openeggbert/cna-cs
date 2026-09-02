using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// <c>Clear(Color)</c> clears the depth buffer as well as the colour target.
///
/// This is the one-argument overload nearly every XNA game calls once per frame, and until
/// 2026-09-02 this binding implemented it as a colour-only clear. Nothing noticed, because a clear
/// that visibly works and geometry that visibly does not look like two separate problems: the
/// window filled with the clear colour every frame, so the device was plainly alive, while every
/// depth-tested draw was silently rejected against a depth buffer that had never been written.
///
/// `cna-cs-samples` CSSAMPLE-001 -- the unmodified original XNA PrimitivesSample -- is the case in
/// point. Its stars, ships and sun did not appear at all, while the identical geometry drawn
/// through the identical `PrimitiveBatch` into a depth-less `RenderTarget2D` appeared normally.
/// The C++ port of the same sample renders correctly on the same CNA build, which is what located
/// the defect on the managed side.
///
/// FNA is the authority for the contract (`src/Graphics/GraphicsDevice.cs`):
/// <c>Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, color,
/// Viewport.MaxDepth, 0)</c>. CNA's own C++ layer already agreed.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class ClearColorDepthTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private const int Size = 8;

    /// <summary>
    /// A quad drawn at mid-depth survives <c>Clear(Color)</c>, but not a target-only clear.
    ///
    /// The depth buffer is deliberately pre-loaded with 0 -- the most hostile value there is under
    /// <c>LessEqual</c>, and the value an uninitialised buffer tends to hold, which is why the
    /// sample failed from its very first frame rather than its second. The quad's vertex z of 0.5
    /// sits strictly between 0 and 1 under either clip convention, so the test does not depend on
    /// whether the pipeline maps NDC z from [0,1] or [-1,1].
    ///
    /// The three cases run in one test on purpose. Case 3 is what makes cases 1 and 2 mean
    /// something: if the fixture were not actually depth-sensitive, all three would be white and a
    /// pass would be vacuous.
    /// </summary>
    [Native3DFact]
    public void Clear_Color_ClearsDepthSoDepthTestedGeometrySurvives()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var target = new RenderTarget2D(
                device, Size, Size, false, SurfaceFormat.Color, DepthFormat.Depth24, 0,
                RenderTargetUsage.DiscardContents);

            if (!CnaNativeProbe.SupportsRenderTargetReadback(device, output))
            {
                CnaNativeProbe.AssertRefusedAsNotSupported(
                    "reading a render target back",
                    () => target.GetData(new Color[Size * Size]),
                    output);
                return;
            }

            using var effect = new BasicEffect(device) { VertexColorEnabled = true };
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, Size, Size, 0, 0, 1);

            var quad = new[]
            {
                new VertexPositionColor(new Vector3(0, 0, 0.5f), Color.White),
                new VertexPositionColor(new Vector3(Size, 0, 0.5f), Color.White),
                new VertexPositionColor(new Vector3(0, Size, 0.5f), Color.White),
                new VertexPositionColor(new Vector3(Size, 0, 0.5f), Color.White),
                new VertexPositionColor(new Vector3(Size, Size, 0.5f), Color.White),
                new VertexPositionColor(new Vector3(0, Size, 0.5f), Color.White),
            };

            int LitPixelsAfter(Action clear)
            {
                device.SetRenderTarget(target);
                try
                {
                    // Depth 0 beats anything a LessEqual test can draw, so whatever the clear under
                    // test does to the depth buffer is the only thing that decides the outcome.
                    device.Clear(ClearOptions.DepthBuffer, Color.Black, 0f, 0);
                    clear();

                    device.DepthStencilState = DepthStencilState.Default;
                    device.RasterizerState = RasterizerState.CullNone;
                    device.BlendState = BlendState.Opaque;
                    effect.CurrentTechnique.Passes[0].Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
                }
                finally
                {
                    device.SetRenderTarget(null);
                }

                var pixels = new Color[Size * Size];
                target.GetData(pixels);
                return pixels.Count(p => p.R > 127);
            }

            int simple = LitPixelsAfter(() => device.Clear(Color.Black));
            int explicitAll = LitPixelsAfter(() => device.Clear(
                ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil,
                Color.Black, 1f, 0));
            int targetOnly = LitPixelsAfter(() => device.Clear(ClearOptions.Target, Color.Black, 1f, 0));

            output.WriteLine($"Clear(Color)                     : {simple} lit of {Size * Size}");
            output.WriteLine($"Clear(Target|Depth|Stencil, 1.0) : {explicitAll} lit of {Size * Size}");
            output.WriteLine($"Clear(Target only)               : {targetOnly} lit of {Size * Size}");

            Assert.True(
                targetOnly == 0,
                $"a target-only clear left the hostile depth of 0 in place, so the quad must be " +
                $"rejected, but {targetOnly} pixels were lit. This test cannot prove anything about " +
                $"the other two cases unless this one is dark.");

            Assert.True(
                explicitAll == Size * Size,
                $"the explicit Target|DepthBuffer|Stencil clear should have reset depth to 1.0 and " +
                $"let the whole quad through, but only {explicitAll} of {Size * Size} pixels were lit.");

            Assert.True(
                simple == Size * Size,
                $"Clear(Color) must clear depth as well as colour -- XNA and FNA define it as " +
                $"Clear(Target|DepthBuffer|Stencil, color, Viewport.MaxDepth, 0). Only {simple} of " +
                $"{Size * Size} pixels survived the depth test, so the depth buffer still held the " +
                $"hostile 0 this test wrote before it.");
        });
    }
}
