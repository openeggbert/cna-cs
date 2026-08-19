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
public class GraphicsIntegrationTests(ITestOutputHelper output)
{
    /// <summary>Runs <paramref name="body"/> inside a real frame, with a real device, and surfaces
    /// whatever it threw as a test failure rather than letting it unwind through native.</summary>
    private static void InsideAFrame(Action<GraphicsDevice> body)
    {
        using var game = new FrameRunner(body);

        for (int i = 0; i < 4 && !game.Ran; i++)
        {
            game.RunOneFrame();
        }

        if (game.Failure is { } failure)
        {
            throw new Xunit.Sdk.XunitException($"The body threw inside the frame: {failure}");
        }

        Assert.True(game.Ran, "The frame never ran, so nothing was exercised.");
    }

    private sealed class FrameRunner(Action<GraphicsDevice> body) : CNA.Game
    {
        public bool Ran { get; private set; }

        public Exception? Failure { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            if (!Ran)
            {
                Ran = true;
                try
                {
                    body(GraphicsDevice);
                }
                catch (Exception ex)
                {
                    Failure = ex;
                }
            }

            Exit();
            base.Update(gameTime);
        }
    }

    [NativeFact]
    public void GraphicsDevice_IsReachable_AndReportsAViewport()
    {
        InsideAFrame(device =>
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
        InsideAFrame(device =>
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
        InsideAFrame(device =>
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
        InsideAFrame(device =>
        {
            using var texture = new Texture2D(device, 2, 2);
            texture.SetData(new Color[4]);

            var thrown = Assert.Throws<NotSupportedException>(() => texture.GetData(new long[4]));

            output.WriteLine(thrown.Message);
            Assert.Contains("CNA_TextureDataType", thrown.Message, StringComparison.Ordinal);
        });
    }

    /// <summary>Clearing is the simplest draw call there is, and the one HelloGame's first success
    /// criterion is built on. If this throws, nothing renders.</summary>
    [NativeFact]
    public void GraphicsDevice_Clear_Succeeds()
    {
        InsideAFrame(device => device.Clear(Color.CornflowerBlue));
    }

    /// <summary>A full SpriteBatch pass: Begin, one Draw, End. This is the whole 2D pipeline, and
    /// the vertical slice `samples/HelloGame` exists to demonstrate.</summary>
    [NativeFact]
    public void SpriteBatch_BeginDrawEnd_Succeeds()
    {
        InsideAFrame(device =>
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
        InsideAFrame(_ =>
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
        InsideAFrame(_ =>
        {
            string path = TitleLocation.Path;
            output.WriteLine($"TitleLocation.Path = '{path}'");

            Assert.False(string.IsNullOrWhiteSpace(path), "TitleLocation.Path came back empty.");
        });
    }
}
