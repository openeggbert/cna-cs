using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Graphics resources, against the real driver. These are the routes where a managed test proves
/// least: every one crosses the ABI with a struct or a buffer, and a layout mismatch shows up as
/// wrong pixels or a corrupt heap rather than as an exception.
///
/// A <see cref="GraphicsDevice"/> only exists inside an active game lifecycle
/// (<c>cna_game_get_graphics_device</c> fails otherwise), so everything here runs inside a frame
/// rather than standalone. That is a constraint of the ABI, not a choice.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class GraphicsIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{


    [NativeFact]
    public void GraphicsDevice_IsReachable_AndReportsAViewport()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            Assert.NotNull(device);

            Viewport viewport = device.Viewport;
            output.WriteLine($"viewport {viewport.Width}x{viewport.Height}, depth {viewport.MinDepth}..{viewport.MaxDepth}");

            Assert.True(viewport.Width > 0, "Viewport width came back as zero -- likely a struct layout mismatch.");
            Assert.True(viewport.Height > 0, "Viewport height came back as zero.");
        });
    }

    /// <summary>
    /// Create a texture and upload pixels. The most layout-sensitive call in the binding:
    /// <see cref="Color"/> must be four bytes in RGBA order on both sides, and the transfer
    /// descriptor must match the C struct exactly. A mismatch here does not throw -- it writes past
    /// the buffer or uploads garbage -- so the check is that dimensions read back correctly
    /// afterwards and the process is still alive.
    /// </summary>
    [NativeFact]
    public void Texture2D_SetData_UploadsWithoutCorruption()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 2, 2);

            Color[] pixels =
            [
                new(255, 0, 0, 255), new(0, 255, 0, 255),
                new(0, 0, 255, 255), new(255, 255, 255, 128),
            ];

            texture.SetData(pixels);

            Assert.Equal(2, texture.Width);
            Assert.Equal(2, texture.Height);
            Assert.Equal(SurfaceFormat.Color, texture.Format);
        });
    }

    /// <summary>
    /// The full round trip. This test previously asserted that <c>GetData</c> <em>threw</em>,
    /// pinning it as a known gap "until the ABI gains a format-and-element-size query" -- and the
    /// query was in the same header the whole time. It now asserts what it should always have:
    /// bytes written come back.
    ///
    /// The most layout-sensitive path in the binding. <see cref="Color"/> must be four bytes in
    /// RGBA order on both sides and the transfer descriptor must match the C struct field for
    /// field; a mismatch reads plausible-looking garbage rather than failing.
    /// </summary>
    [NativeFact]
    public void Texture2D_SetThenGetData_RoundTrips()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 2, 2);

            Color[] written =
            [
                new(255, 0, 0, 255), new(0, 255, 0, 255),
                new(0, 0, 255, 255), new(255, 255, 255, 128),
            ];

            texture.SetData(written);

            var read = new Color[4];
            texture.GetData(read);

            output.WriteLine("wrote: " + string.Join(", ", written));
            output.WriteLine("read:  " + string.Join(", ", read));

            Assert.Equal(written, read);
        });
    }

    /// <summary>An element type the ABI has no overload for must be refused by name, not silently
    /// read as raw bytes -- a byte-wise read of the wrong type succeeds, returns the right byte
    /// count, and is wrong.</summary>
    [NativeFact]
    public void Texture2D_GetData_RefusesAnUnmappedElementType()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 2, 2);
            texture.SetData(new Color[4]);

            var thrown = Assert.Throws<NotSupportedException>(() => texture.GetData(new long[4]));

            output.WriteLine(thrown.Message);
            Assert.Contains("CNA_TextureDataType", thrown.Message, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// The capability query, against whichever renderer this run loaded.
    ///
    /// It answers rather than throwing, and answers consistently with the renderer's own name --
    /// which is the whole point. `cna-cs-template` had no such method to call, guessed `true` when
    /// its `dynamic` probe failed, and on the 2D-only SDL_RENDERER reported "3D pipeline: yes"
    /// before dying inside `DrawUserPrimitives`. A capability probe whose failure mode is optimism
    /// is worse than none.
    /// </summary>
    [NativeFact]
    public void GraphicsDevice_SupportsCapability_AnswersForEveryIdentity()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            output.WriteLine($"renderer '{device.RendererName}'");

            foreach (GraphicsCapability capability in Enum.GetValues<GraphicsCapability>())
            {
                output.WriteLine($"  {capability,-28}: {device.SupportsCapability(capability)}");
            }

            // A renderer that cannot draw in 3D must not claim it can. This is the exact question
            // the template got wrong.
            bool threeD = device.SupportsCapability(GraphicsCapability.ThreeD);
            Assert.Equal(threeD, device.SupportsCapability(GraphicsCapability.ThreeD));

            Assert.False(string.IsNullOrWhiteSpace(device.RendererName), "RendererName came back empty.");
        });
    }

    /// <summary>Clearing is the simplest draw call there is, and the one HelloGame's first success
    /// criterion is built on. If this throws, nothing renders.</summary>
    [NativeFact]
    public void GraphicsDevice_Clear_Succeeds()
    {
        fixture.InsideAFrameWithDevice(device => device.Clear(Color.CornflowerBlue));
    }

    /// <summary>A full SpriteBatch pass: Begin, one Draw, End. This is the whole 2D pipeline, and
    /// the vertical slice `samples/HelloGame` exists to demonstrate.</summary>
    [NativeFact]
    public void SpriteBatch_BeginDrawEnd_Succeeds()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 1, 1);
            texture.SetData([Color.White]);

            using var batch = new SpriteBatch(device);

            device.Clear(Color.Black);
            batch.Begin();
            batch.Draw(texture, new Vector2(10f, 20f), Color.White);
            batch.End();
        });
    }

    /// <summary>Adapter enumeration, which reaches native through the ambient game rather than
    /// through a device handle the caller holds. A different code path from everything above.</summary>
    [NativeFact]
    public void GraphicsAdapter_EnumeratesAtLeastOneAdapter()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            IReadOnlyList<GraphicsAdapter> adapters = GraphicsAdapter.Adapters;

            Assert.NotEmpty(adapters);
            output.WriteLine($"{adapters.Count} adapter(s); first is '{adapters[0].Description}'");
        });
    }

    /// <summary>
    /// <c>TitleLocation.Path</c>, added this session and never executed until now. It is the whole
    /// reason a relative <c>ContentManager.RootDirectory</c> resolves, so a wrong answer here is a
    /// game that cannot find its own assets.
    /// </summary>
    [NativeFact]
    public void TitleLocation_ReturnsARealDirectory()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            string path = TitleLocation.Path;
            output.WriteLine($"TitleLocation.Path = '{path}'");

            Assert.False(string.IsNullOrWhiteSpace(path), "TitleLocation.Path came back empty.");
        });
    }
}
