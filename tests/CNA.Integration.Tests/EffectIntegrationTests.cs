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
[Collection(NativeGameCollection.Name)]
public class EffectIntegrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{


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
        fixture.InsideAFrameWithDevice(device =>
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
        fixture.InsideAFrameWithDevice(device =>
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

        fixture.InsideAFrame(game =>
        {
            try
            {
                game.Content.Load<Effect>("no-such-effect-asset");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        Assert.NotNull(caught);
        output.WriteLine($"{caught!.GetType().Name}: {caught.Message}");

        Assert.IsNotType<NotSupportedException>(caught);
        Assert.Contains("Io", caught.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A real custom shader, from source, on a renderer that says it supports them.
    ///
    /// This is the capability the project's "custom shaders are blocked" note never distinguished.
    /// CustomEffects and CompiledEffects are separate identities and they differ in practice: the
    /// SOFTWARE renderer reports CustomEffects true and CompiledEffects false. A game shipping a
    /// compiled .fx still cannot load it there -- and a game that can supply source now can, which
    /// nothing in this binding could do until the route was bound.
    ///
    /// The source is written for whichever dialect the device names, because the header is explicit
    /// that guessing from the renderer's identity is unsafe. An unknown dialect skips rather than
    /// guesses.
    /// </summary>
    [NativeFactRequiring(GraphicsCapability.CustomEffects)]
    public void Effect_FromShaderSource_CompilesOnARendererThatSupportsIt()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            ShaderDialect dialect = device.ShadingDialect;
            output.WriteLine($"renderer wants {dialect}");

            if (dialect is ShaderDialect.Unknown)
            {
                output.WriteLine("no dialect declared; supplying text for a guessed one is what the header warns against");
                return;
            }

            string version = dialect switch
            {
                ShaderDialect.GlslEs => "#version 300 es\nprecision mediump float;",
                _ => "#version 330 core",
            };

            string vertex = version + "\nin vec3 a_position;\nvoid main() { gl_Position = vec4(a_position, 1.0); }";
            string fragment = version + "\nout vec4 o_color;\nvoid main() { o_color = vec4(1.0, 0.0, 0.0, 1.0); }";

            using var effect = new Effect(device, vertex, fragment);

            effect.Apply();

            output.WriteLine(
                $"compiled: {effect.Parameters.Count} parameter(s), {effect.Techniques.Count} technique(s), " +
                $"{effect.CurrentTechnique.Passes.Count} pass(es)");

            Assert.True(effect.Techniques.Count > 0, "A compiled effect with no techniques cannot be applied.");
        });
    }

    /// <summary>
    /// Malformed source. <b>Currently accepted</b>, which is what this records.
    ///
    /// `cna_shader_effect_create` returns success for "this is not a shader" on a renderer
    /// reporting CustomEffects, so a game gets a live effect object for text that cannot draw.
    /// Reported upstream with the two readings I cannot separate from here -- the renderer accepts
    /// source without compiling it, or the compile is deferred to first use and the constructor is
    /// not where it reports.
    ///
    /// Asserting the acceptance would encode a defect as expected and pass forever, which this
    /// project has already been caught doing once. So it is skipped, asserting the *correct*
    /// behaviour, and deleting the skip is the verification.
    /// </summary>
    [NativeFact("Upstream: cna_shader_effect_create accepts text that is not a shader and returns a live effect. Reported; remove this Skip to verify the fix.")]
    public void Effect_FromShaderSource_RejectsMalformedText()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            var thrown = Assert.Throws<CnaException>(() =>
                new Effect(device, "this is not a shader", "neither is this"));

            output.WriteLine(thrown.Message);
        });
    }
}
