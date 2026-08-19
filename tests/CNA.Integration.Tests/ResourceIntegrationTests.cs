using CNA.Audio;
using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The remaining resource types, one real create-and-read each.
///
/// Cube and volume textures, cube render targets, display-mode enumeration, presentation
/// parameters, the service container, and the two sound-effect instance shapes. Each crosses the
/// ABI with its own create/info/destroy trio, so "Texture2D works" says nothing about any of them --
/// a wrong info struct here reports plausible dimensions and is wrong.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class ResourceIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    [Native3DFact]
    public void TextureCube_CreatesAndReportsItsSize()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var cube = new TextureCube(device, 8);

            output.WriteLine($"size={cube.Size} levels={cube.LevelCount} format={cube.Format}");

            Assert.Equal(8, cube.Size);
            Assert.True(cube.LevelCount >= 1, "A texture with no mip levels cannot be sampled.");
        });
    }

    /// <summary>Uploading one face. A cube's six faces are separate transfers, and getting the face
    /// selector wrong writes the right pixels to the wrong side -- which nothing but a read-back or
    /// a render would show.</summary>
    [Native3DFact]
    public void TextureCube_AcceptsPerFaceData()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var cube = new TextureCube(device, 2);

            var face = new Color[2 * 2];
            Array.Fill(face, Color.CornflowerBlue);

            foreach (CubeMapFace side in Enum.GetValues<CubeMapFace>())
            {
                cube.SetData(side, face);
            }

            Assert.Equal(2, cube.Size);
        });
    }

    /// <summary>Volume textures are their own capability, separate from having a 3D pipeline at
    /// all: SOFTWARE reports ThreeD and still has no real volume storage.</summary>
    [NativeFactRequiring(GraphicsCapability.Texture3D)]
    public void Texture3D_CreatesAndReportsItsDimensions()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var volume = new Texture3D(device, 4, 4, 2);

            output.WriteLine($"{volume.Width}x{volume.Height}x{volume.Depth} format={volume.Format}");

            Assert.Equal(4, volume.Width);
            Assert.Equal(4, volume.Height);
            Assert.Equal(2, volume.Depth);
        });
    }

    [Native3DFact]
    public void RenderTargetCube_CreatesAndReportsItsProperties()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var target = new RenderTargetCube(
                device, 16, false, SurfaceFormat.Color, DepthFormat.Depth24);

            output.WriteLine(
                $"size={target.Size} depth={target.DepthStencilFormat} usage={target.RenderTargetUsage} " +
                $"msaa={target.MultiSampleCount} lost={target.IsContentLost}");

            Assert.Equal(16, target.Size);
        });
    }

    /// <summary>
    /// Display-mode enumeration, which crosses as an array of versioned structs -- the shape where a
    /// stride mismatch reports plausible-looking resolutions that are all wrong.
    /// </summary>
    [NativeFact]
    public void GraphicsAdapter_EnumeratesDisplayModes()
    {
        fixture.InsideAFrame(_ =>
        {
            GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;

            DisplayMode current = adapter.CurrentDisplayMode;
            output.WriteLine($"current {current.Width}x{current.Height} {current.Format} aspect={current.AspectRatio:F3}");

            Assert.True(current.Width > 0 && current.Height > 0, "The current display mode has no size.");

            DisplayModeCollection supported = adapter.SupportedDisplayModes;
            output.WriteLine($"{supported.Count} supported mode(s)");

            foreach (DisplayMode mode in supported.Take(4))
            {
                Assert.True(mode.Width > 0 && mode.Height > 0, $"Mode {mode.Width}x{mode.Height} has no size.");
            }
        });
    }

    /// <summary>The device's presentation parameters, read from the live device rather than from a
    /// hand-built instance.</summary>
    [NativeFact]
    public void GraphicsDevice_PresentationParameters_DescribeTheBackBuffer()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            PresentationParameters presentation = device.PresentationParameters;

            output.WriteLine(
                $"{presentation.BackBufferWidth}x{presentation.BackBufferHeight} {presentation.BackBufferFormat} " +
                $"depth={presentation.DepthStencilFormat} interval={presentation.PresentationInterval} " +
                $"fullscreen={presentation.IsFullScreen}");

            Assert.True(presentation.BackBufferWidth > 0, "A back buffer with no width cannot be presented.");
            Assert.Equal(presentation.Bounds.Width, presentation.BackBufferWidth);

            // Clone is a real XNA member and a separate native route.
            PresentationParameters clone = presentation.Clone();
            Assert.Equal(presentation.BackBufferWidth, clone.BackBufferWidth);
        });
    }

    /// <summary>
    /// The service container. Managed-only by necessity -- <c>runtime_components.h</c> states that
    /// nothing in it can register a service -- so this exercises the parallel container the binding
    /// keeps, which is the thing a ported game actually uses.
    /// </summary>
    [NativeFact]
    public void GameServiceContainer_AddsAndResolvesAService()
    {
        fixture.InsideAFrame(game =>
        {
            var service = new object();

            game.Services.AddService(typeof(object), service);
            Assert.Same(service, game.Services.GetService(typeof(object)));

            game.Services.RemoveService(typeof(object));
            Assert.Null(game.Services.GetService(typeof(object)));
        });
    }

    /// <summary>A sound effect instance through its whole state machine. Separate native object
    /// from the SoundEffect that created it.</summary>
    [NativeFact]
    public void SoundEffectInstance_MovesThroughItsStates()
    {
        fixture.InsideAFrame(_ =>
        {
            var pcm = new byte[44100 / 20 * 2];
            using var effect = new SoundEffect(pcm, 44100, AudioChannels.Mono);
            using SoundEffectInstance instance = effect.CreateInstance();

            Assert.Equal(SoundState.Stopped, instance.State);

            instance.Play();
            output.WriteLine($"after Play: {instance.State}");

            instance.Pause();
            instance.Resume();
            instance.Stop();

            Assert.Equal(SoundState.Stopped, instance.State);
        });
    }

    /// <summary>
    /// A dynamic instance, which is the streaming shape: a game submits buffers as they are
    /// produced rather than playing a fixed one.
    ///
    /// Its buffer refills are driven by <c>FrameworkDispatcher.Update</c>, which until CBIND-068
    /// never ran for a C consumer -- the same fix that made components tick. So this is exercising
    /// a path that was dead until very recently.
    /// </summary>
    [NativeFact]
    public void DynamicSoundEffectInstance_AcceptsASubmittedBuffer()
    {
        fixture.InsideAFrame(_ =>
        {
            using var dynamicInstance = new DynamicSoundEffectInstance(44100, AudioChannels.Mono);

            TimeSpan duration = dynamicInstance.GetSampleDuration(44100 * 2);
            int size = dynamicInstance.GetSampleSizeInBytes(TimeSpan.FromSeconds(1));
            output.WriteLine($"1s of samples = {size} bytes; 44100 samples = {duration}");

            Assert.True(size > 0, "A one-second buffer cannot be zero bytes.");

            dynamicInstance.SubmitBuffer(new byte[size / 10]);
            output.WriteLine($"pending buffers: {dynamicInstance.PendingBufferCount}");
        });
    }
}
