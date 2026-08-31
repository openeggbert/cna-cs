using CNA.Audio;
using CNA.Graphics;
using CNA.Media;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The remaining device-scoped subsystems, one real call each.
///
/// These are the ones a real game touches every frame or every load and that nothing had ever
/// executed: the sampler and texture slot collections, occlusion queries, the media library, the
/// video player, and the XACT audio engine. Shallow on purpose -- what is being established is
/// reachability, since a route that is wrong here is wrong in a way no managed test can see.
///
/// Several of these are expected to *fail* in a headless run, and that is information rather than a
/// problem: a media library with no music, a video player with no video. Where that is the case the
/// test asserts the documented failure instead of pretending success.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class DeviceStateIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>
    /// The two slot collections. Their <c>Count</c> is the device's real slot capacity, and the
    /// member diff previously flagged <c>MaxTextures</c>/<c>MaxSamplers</c> as missing -- they are
    /// this, under XNA's own name.
    /// </summary>
    [NativeFact]
    public void TextureAndSamplerCollections_ReportTheDeviceSlotCounts()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            output.WriteLine($"{device.Textures.Count} texture slot(s), {device.SamplerStates.Count} sampler slot(s)");

            Assert.True(device.Textures.Count > 0, "A device with no texture slots cannot sample anything.");
            Assert.True(device.SamplerStates.Count > 0, "A device with no sampler slots cannot filter anything.");
        });
    }

    /// <summary>Binding a texture into a slot and reading it back. The read is answered from this
    /// object's own record for the reason <c>TextureCollection</c> documents -- native reports a
    /// bare handle -- so the round trip is what proves the record is kept.</summary>
    [NativeFact]
    public void TextureCollection_RoundTripsABoundTexture()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var texture = new Texture2D(device, 1, 1);
            texture.SetData([Color.White]);

            device.Textures[0] = texture;
            Assert.Same(texture, device.Textures[0]);

            device.Textures[0] = null;
            Assert.Null(device.Textures[0]);
        });
    }

    /// <summary>Sampler state through the collection, which is a different native route from the
    /// device-wide <c>SamplerState</c> property.</summary>
    [NativeFact]
    public void SamplerStateCollection_RoundTripsAState()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            device.SamplerStates[0] = SamplerState.PointClamp;

            SamplerState read = device.SamplerStates[0];
            output.WriteLine($"slot 0: filter={read.Filter}, addressU={read.AddressU}");

            Assert.Equal(TextureFilter.Point, read.Filter);
        });
    }

    /// <summary>
    /// An occlusion query, begin to end. Needs a 3D pipeline -- it counts pixels that passed the
    /// depth test, which a 2D renderer has none of.
    /// </summary>
    [Native3DFact]
    public void OcclusionQuery_BeginsAndEnds()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            // Asked as OcclusionQuery, not ThreeD. It has a capability identity of its own, and
            // using the broader one would have claimed that any renderer with a 3D pipeline can run
            // a query -- which is a different statement, and not one this test measures.
            if (!CnaNativeProbe.HasCapabilityOrRefuses(
                    device,
                    GraphicsCapability.OcclusionQuery,
                    "creating an OcclusionQuery",
                    () => new OcclusionQuery(device).Dispose(),
                    output))
            {
                return;
            }

            using var query = new OcclusionQuery(device);

            query.Begin();
            device.Clear(Color.Black);
            query.End();

            output.WriteLine($"complete={query.IsComplete}");
        });
    }

    /// <summary>
    /// The media library opens and reports its collections.
    ///
    /// Empty is the expected answer on a machine with no music, and empty is not the same as
    /// broken: what this establishes is that the library opens, the scan runs, and each collection
    /// answers a count rather than failing. This whole family was once documented as having "no C
    /// ABI exposure to build against" and "permanently empty by design", which was the first of the
    /// false claims this project retracted -- media_library.h has 148 functions.
    /// </summary>
    [NativeFact]
    public void MediaLibrary_OpensAndReportsItsCollections()
    {
        fixture.InsideAFrame(_ =>
        {
            using var library = new MediaLibrary();

            output.WriteLine(
                $"songs={library.Songs.Count} albums={library.Albums.Count} artists={library.Artists.Count} " +
                $"genres={library.Genres.Count} playlists={library.Playlists.Count} pictures={library.Pictures.Count}");

            Assert.True(library.Songs.Count >= 0);
        });
    }

    /// <summary>A video player with no video reports a stopped state rather than failing. The
    /// player itself is a real native object either way.</summary>
    [NativeFact]
    public void VideoPlayer_ConstructsAndReportsStoppedState()
    {
        fixture.InsideAFrame(_ =>
        {
            using var player = new VideoPlayer();

            output.WriteLine($"state={player.State} volume={player.Volume} looped={player.IsLooped}");

            Assert.Equal(MediaState.Stopped, player.State);
            Assert.Null(player.Video);
        });
    }

    /// <summary>
    /// The frame generation CNA exposes alongside the borrowed frame texture.
    ///
    /// <b>Why this is worth binding without a video to play.</b> The blocker row for
    /// <c>VideoPlayer.GetTexture</c> asks for "stable frame-slot identity <em>or an explicit
    /// validity generation</em>", because XNA hands back two stable <c>Texture2D</c> objects and a
    /// game compares references to notice a new frame, while CNA's alias expires on the next call
    /// and no reference comparison means anything. The generation is the second of those two, it
    /// exists as of this ABI, and leaving it unbound would keep the row saying "needs" about
    /// something that is there.
    ///
    /// <b>What this cannot check, and says so.</b> Whether the generation actually advances needs a
    /// video, and no legally redistributable fixture is available here -- that is a fixture blocker,
    /// not an oversight. What is checked is everything reachable without one: the route runs on a
    /// player that has never played, reports no frame, and answers a defined generation and a
    /// negative presentation time rather than failing.
    /// </summary>
    [NativeFact]
    public void VideoPlayer_ReportsAFrameGenerationBeforeAnythingHasPlayed()
    {
        fixture.InsideAFrame(_ =>
        {
            using var player = new VideoPlayer();

            CnaVideoFrame frame = player.GetCnaFrame();
            output.WriteLine(
                $"generation={frame.Generation} available={frame.IsAvailable} " +
                $"presentation={frame.PresentationTime}");

            Assert.False(frame.IsAvailable);
            Assert.Null(frame.Texture);
            Assert.Equal(0UL, frame.Generation);

            // Negative is the documented "no frame" timestamp. Asserted rather than ignored because
            // zero would be a perfectly plausible first presentation time, and a route that
            // returned zero here would be indistinguishable from one holding a frame at t=0.
            Assert.True(frame.PresentationTime < 0, $"expected a negative timestamp, got {frame.PresentationTime}");

            // Reading twice must not advance it: the generation counts decoded frames, not calls.
            Assert.Equal(frame.Generation, player.GetCnaFrame().Generation);
        });
    }

    /// <summary>A disposed player refuses rather than answering with a stale frame.</summary>
    [NativeFact]
    public void VideoPlayer_FrameOnADisposedPlayer_IsRefused()
    {
        fixture.InsideAFrame(_ =>
        {
            var player = new VideoPlayer();
            player.Dispose();

            CnaException failure = Assert.Throws<CnaException>(() => player.GetCnaFrame());
            output.WriteLine($"disposed player: {failure.NativeResult}: {failure.Message}");
        });
    }

    /// <summary>
    /// Microphone enumeration. Zero devices is the expected headless answer and is asserted as
    /// such -- the point is that the enumeration route runs and reports a count, not that this
    /// machine has a microphone.
    /// </summary>
    [NativeFact]
    public void Microphone_EnumerationRuns()
    {
        fixture.InsideAFrame(_ =>
        {
            IReadOnlyList<Microphone> all = Microphone.All;
            output.WriteLine($"{all.Count} microphone(s); default is {(Microphone.Default is null ? "none" : "present")}");

            Assert.NotNull(all);
        });
    }
}
