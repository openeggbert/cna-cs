using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// The compiled-effect routes, which were recorded as this binding's single largest functional
/// blocker -- "every ported XNA 3D game with a custom shader stops here", on the strength of a
/// header sentence saying <c>cna_effect_create_compiled</c> answered <c>NOT_SUPPORTED</c> "while
/// native CNA bytecode loading is unavailable".
///
/// That sentence had outlived its implementation. These tests exist to keep the question settled by
/// measurement rather than by prose, in either direction: if the capability is absent on this
/// renderer they assert the documented failure, and if it is present they assert it works. What they
/// will not do is let a claim about it sit unchecked again.
/// </summary>
public class EffectIntegrationTests(ITestOutputHelper output)
{
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

    /// <summary>
    /// The constructor exists and reaches native. Garbage bytes are the input on purpose: no
    /// compiled shader fixture ships with this repository, and what needs proving here is that the
    /// call crosses the ABI and comes back with a *judgement* rather than an
    /// <see cref="EntryPointNotFoundException"/> or a crash.
    ///
    /// Either documented rejection is a pass. `INVALID_ARGUMENT` means the header parser ran and
    /// refused these bytes; `NOT_SUPPORTED` means the renderer has no compiled-effect capability.
    /// Both prove the route is live. What would fail this test is the route not being there.
    /// </summary>
    [NativeFact]
    public void Effect_FromBytecode_ReachesNativeAndJudgesTheInput()
    {
        InsideAFrame(device =>
        {
            byte[] notAnEffect = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

            var thrown = Assert.Throws<CnaException>(() => new Effect(device, notAnEffect));

            output.WriteLine(thrown.Message);
            Assert.True(
                thrown.Message.Contains("InvalidArgument", StringComparison.Ordinal) ||
                thrown.Message.Contains("NotSupported", StringComparison.Ordinal),
                $"Expected a documented rejection of malformed bytecode, got: {thrown.Message}");
        });
    }

    /// <summary>Argument validation happens before any native call, so it holds on every renderer
    /// regardless of capability.</summary>
    [NativeFact]
    public void Effect_FromBytecode_RejectsEmptyAndNullInput()
    {
        InsideAFrame(device =>
        {
            Assert.Throws<ArgumentNullException>(() => new Effect(device, null!));
            Assert.Throws<ArgumentException>(() => new Effect(device, []));
        });
    }

    /// <summary>
    /// <c>Content.Load&lt;Effect&gt;</c> is wired at all -- it used to fall through to
    /// "Unsupported content type", which is what made every custom-shader game stop.
    ///
    /// A missing asset must fail as <em>IO</em>, the documented result for an asset that is not
    /// there. That distinguishes "the route is wired and the file is missing" from "the type is not
    /// handled", which is exactly the confusion this test is here to prevent.
    /// </summary>
    [NativeFact]
    public void ContentManager_LoadEffect_IsWired_AndReportsAMissingAssetAsIo()
    {
        Exception? caught = null;

        using (var probe = new ContentProbe(ex => caught = ex))
        {
            for (int i = 0; i < 4 && !probe.Ran; i++)
            {
                probe.RunOneFrame();
            }
        }

        Assert.NotNull(caught);
        output.WriteLine($"{caught!.GetType().Name}: {caught.Message}");

        Assert.IsNotType<NotSupportedException>(caught);
        Assert.Contains("Io", caught.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ContentProbe(Action<Exception> report) : CNA.Game
    {
        public bool Ran { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            if (!Ran)
            {
                Ran = true;
                try
                {
                    Content.Load<Effect>("no-such-effect-asset");
                }
                catch (Exception ex)
                {
                    report(ex);
                }
            }

            Exit();
            base.Update(gameTime);
        }
    }
}
