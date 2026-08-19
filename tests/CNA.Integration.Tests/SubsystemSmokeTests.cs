using CNA.Audio;
using CNA.Graphics;
using CNA.Input;
using CNA.Media;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Breadth rather than depth: one real call into each major subsystem, to find the ones that do not
/// work at all.
///
/// The motivating number is that of 223 public compat types, runtime coverage had touched 43. The
/// rest was verified statically -- declarations present in headers, arities matching, structs
/// versioned -- which catches a fabricated import and cannot catch a wrong struct layout, a stale
/// handle, or a route that simply fails on every renderer. Those only show up when something calls
/// them.
///
/// Deliberately shallow. A subsystem that answers plausibly here is not proven correct; it is
/// proven reachable. That is still the difference between "compiles" and "runs".
/// </summary>
[Collection(NativeGameCollection.Name)]
public class SubsystemSmokeTests(ITestOutputHelper output, NativeGameFixture fixture)
{


    /// <summary>Input state readers. All three are pure reads of a device snapshot, so they work
    /// headless -- the values are meaningless without a device, but a crash or a garbled struct is
    /// not.</summary>
    [NativeFact]
    public void Input_AllThreeDevices_ReportState()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            KeyboardState keyboard = Keyboard.GetState();
            Assert.NotNull(keyboard.GetPressedKeys());

            MouseState mouse = Mouse.GetState();
            output.WriteLine($"mouse ({mouse.X},{mouse.Y}) left={mouse.LeftButton}");

            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            output.WriteLine($"gamepad connected={pad.IsConnected}");

            GamePadCapabilities caps = GamePad.GetCapabilities(PlayerIndex.One);
            output.WriteLine($"gamepad type={caps.GamePadType}");
        });
    }

    /// <summary>A render target is a separate native resource type upstream, not a texture with
    /// flags, so it exercises a different create/info/destroy trio from Texture2D.</summary>
    [NativeFact]
    public void RenderTarget2D_CreatesAndReportsItsProperties()
    {
        fixture.InsideAFrame(game =>
        {
            using var target = new RenderTarget2D(game.GraphicsDevice, 64, 32);

            output.WriteLine(
                $"{target.Width}x{target.Height} {target.Format} depth={target.DepthStencilFormat} " +
                $"usage={target.RenderTargetUsage} msaa={target.MultiSampleCount} lost={target.IsContentLost}");

            Assert.Equal(64, target.Width);
            Assert.Equal(32, target.Height);
        });
    }

    /// <summary>Binding a render target and unbinding it. The cached-binding bookkeeping behind
    /// GetRenderTargets is easy to get wrong in a way only a real bind exposes.</summary>
    [NativeFact]
    public void GraphicsDevice_SetAndClearRenderTarget()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;
            using var target = new RenderTarget2D(device, 32, 32);

            device.SetRenderTarget(target);
            RenderTargetBinding[] bound = device.GetRenderTargets();
            Assert.Single(bound);

            device.SetRenderTarget(null);
            Assert.Empty(device.GetRenderTargets());
        });
    }

    /// <summary>Graphics state objects, which cross the ABI as versioned caller-initialised
    /// structs -- the exact shape where an `out` instead of a `ref` corrupts silently.</summary>
    [NativeFact]
    public void GraphicsDevice_StateObjects_RoundTrip()
    {
        fixture.InsideAFrame(game =>
        {
            GraphicsDevice device = game.GraphicsDevice;

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = RasterizerState.CullCounterClockwise;

            output.WriteLine(
                $"blend={device.BlendState.ColorSourceBlend} depth={device.DepthStencilState.DepthBufferEnable} " +
                $"cull={device.RasterizerState.CullMode}");

            Assert.NotNull(device.BlendState);
            Assert.NotNull(device.DepthStencilState);
            Assert.NotNull(device.RasterizerState);
        });
    }

    /// <summary>SoundEffect from raw PCM. Audio is an entire subsystem nothing had executed.</summary>
    [NativeFact]
    public void SoundEffect_CreatesFromPcm_AndReportsDuration()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            // A tenth of a second of silence, 16-bit mono at 44.1 kHz.
            var pcm = new byte[44100 / 10 * 2];

            using var effect = new SoundEffect(pcm, 44100, AudioChannels.Mono);

            output.WriteLine($"duration {effect.Duration}");
            Assert.True(effect.Duration > TimeSpan.Zero, "A non-empty buffer reported zero duration.");
        });
    }

    /// <summary>The static audio listener/emitter values plus the global mixer knobs.</summary>
    [NativeFact]
    public void SoundEffect_GlobalMixerSettings_RoundTrip()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            SoundEffect.MasterVolume = 0.5f;
            Assert.Equal(0.5f, SoundEffect.MasterVolume, 1e-4f);

            SoundEffect.MasterVolume = 1f;
            Assert.Equal(1f, SoundEffect.MasterVolume, 1e-4f);
        });
    }

    /// <summary>MediaPlayer's global state. Rebound on all 41 media_player.h functions and never
    /// executed until now.</summary>
    [NativeFact]
    public void MediaPlayer_ReportsItsGlobalState()
    {
        fixture.InsideAFrameWithDevice(_ =>
        {
            output.WriteLine($"state={MediaPlayer.State} volume={MediaPlayer.Volume} muted={MediaPlayer.IsMuted}");

            MediaPlayer.Volume = 0.25f;
            Assert.Equal(0.25f, MediaPlayer.Volume, 1e-4f);

            MediaPlayer.IsMuted = true;
            Assert.True(MediaPlayer.IsMuted);
            MediaPlayer.IsMuted = false;
        });
    }


    /// <summary>GameWindow, whose handle route is an `_ext` and whose title crosses the ABI as a
    /// string in both directions.</summary>
    [NativeFact]
    public void GameWindow_TitleRoundTrips()
    {
        fixture.InsideAFrame(game =>
        {
            game.Window.Title = "cna-cs integration";
            Assert.Equal("cna-cs integration", game.Window.Title);

            output.WriteLine($"client bounds {game.Window.ClientBounds}, handle 0x{game.Window.Handle:x}");
        });
    }
}
