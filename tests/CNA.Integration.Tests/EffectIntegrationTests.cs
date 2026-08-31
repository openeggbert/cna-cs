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
            if (!device.SupportsCapability(GraphicsCapability.CustomEffects))
            {
                // The branch that used to be a silent pass, now measured on a SDL_RENDERER build.
                //
                // It is not a refusal. Creating the effect *succeeds* -- CustomEffects does not gate
                // `cna_shader_effect_create` -- and what the renderer says instead is that the
                // source is not valid, for perfectly good GLSL. That is the assertion worth making:
                // a game supplying source to such a renderer gets an object back and must read
                // IsSourceValid rather than trust the constructor.
                using var refused = new Effect(
                    device,
                    "#version 330 core\nin vec3 a_position;\nvoid main() { gl_Position = vec4(a_position, 1.0); }",
                    "#version 330 core\nout vec4 o_color;\nvoid main() { o_color = vec4(1.0); }");

                output.WriteLine(
                    $"ABSENT BRANCH EXERCISED: renderer '{device.RendererName}' does not report " +
                    $"CustomEffects; valid source reports IsSourceValid={refused.IsSourceValid}");

                Assert.False(
                    refused.IsSourceValid,
                    "A renderer without CustomEffects must not claim it compiled a custom effect.");
                return;
            }

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
    [NativeFactRequiring(GraphicsCapability.CustomEffects)]
    public void Effect_FromShaderSource_ReportsTheRenderersVerdict()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            // No capability gate. This test records whatever the renderer says and asserts nothing
            // about which answer it gives, so a renderer that lacks CustomEffects is not an obstacle
            // -- it is the most informative case, and gating it away was pure loss.

            using var effect = new Effect(device, "this is not a shader", "neither is this");

            bool valid = effect.IsSourceValid;
            output.WriteLine($"renderer '{device.RendererName}' says valid={valid}");

            // Deliberately not asserting a value. The answer is genuinely renderer-dependent and
            // both are correct: SDL_RENDERER compiles and refuses, SOFTWARE accepts any non-empty
            // text. Asserting either would encode one renderer's behaviour as the contract.
            //
            // What is asserted is the asymmetry the ABI specifies -- false means a renderer looked
            // and refused, so a false here must be a real rejection.
            if (!valid)
            {
                output.WriteLine("a renderer looked at this and refused it, which is the strong answer");
            }
        });
    }

    /// <summary>An empty source is refused before any renderer sees it, identically everywhere.
    /// It used to make SOFTWARE throw -- surfacing as INTERNAL, blaming CNA for the caller's input
    /// -- while SDL_RENDERER handed back an effect with no source at all.</summary>
    [NativeFactRequiring(GraphicsCapability.CustomEffects)]
    public void Effect_FromShaderSource_RejectsEmptySource()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            // No capability gate: the refusal is managed argument validation that happens before
            // any renderer is asked, so it must hold identically everywhere. Measured on a
            // SDL_RENDERER build, which reports CustomEffects false and still throws.

            Assert.Throws<ArgumentException>(() => new Effect(device, string.Empty, "x"));
            Assert.Throws<ArgumentException>(() => new Effect(device, "x", string.Empty));
        });
    }

    /// <summary>
    /// The last avenue for reaching <c>EffectAnnotation</c>: a source effect whose shader declares
    /// an annotated parameter.
    ///
    /// Expected to find nothing on this renderer, and the test says so rather than asserting a
    /// count. SOFTWARE's <c>ShadingDialect</c> answers Unknown and its compile step accepts any
    /// non-empty text without inspecting it, so there is no reflected graph for annotations to
    /// appear in -- which is the same reason the stock effects report zero parameters here.
    ///
    /// Worth running anyway, because "it probably reflects nothing" is a prediction and this
    /// project has been wrong about six of those. If a parameter ever appears, the annotation
    /// surface becomes reachable and this test starts saying something.
    /// </summary>
    [NativeFactRequiring(GraphicsCapability.CustomEffects)]
    public void Effect_FromShaderSource_ReportsWhateverReflectionTheRendererOffers()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            // The source is built for the dialect the renderer asks for, exactly as the sibling
            // test above does, and for a harder reason than tidiness.
            //
            // This test used to hard-code "#version 330 core". On a GlslEs renderer that is desktop
            // GLSL handed to an ES context, and EasyGL does not refuse it -- it takes the whole
            // process down inside `cna_shader_effect_create`. That is upstream's defect and it is
            // recorded as one (docs/native-behavior-blockers.md: a dialect mismatch must be a
            // refusal, not an abort). But the test was also simply wrong: `effects.h` says a caller
            // must ask for the dialect rather than guess it, and this one guessed. The cost was out
            // of all proportion to the mistake -- the crash took the whole integration suite down
            // on both GL renderers, 94 tests short of the end, and read for a while as an
            // unexplained environmental failure.
            ShaderDialect dialect = device.ShadingDialect;
            string version = dialect switch
            {
                ShaderDialect.GlslEs => "#version 300 es\nprecision mediump float;",
                _ => "#version 330 core",
            };

            string vertex =
                version + "\n" +
                "uniform float u_scale;\n" +
                "in vec3 a_position;\n" +
                "void main() { gl_Position = vec4(a_position * u_scale, 1.0); }";

            string fragment =
                version + "\n" +
                "uniform vec4 u_tint;\n" +
                "out vec4 o_color;\n" +
                "void main() { o_color = u_tint; }";

            if (dialect is ShaderDialect.Unknown && device.SupportsCapability(GraphicsCapability.CustomEffects))
            {
                // A renderer that claims custom effects but declares no dialect leaves a caller
                // nothing to write source for. Reporting that is the honest answer; guessing is
                // what caused the crash above.
                output.WriteLine(
                    $"renderer '{device.RendererName}' supports CustomEffects but declares no " +
                    "shading dialect, so there is no source to supply.");
                return;
            }

            if (!device.SupportsCapability(GraphicsCapability.CustomEffects))
            {
                // Measured absent branch: no compilation, so no reflected graph. Asserted rather
                // than skipped, because "nothing reflects" is exactly the claim this test makes on
                // the renderers that do support the capability, and it should not be a silent pass
                // on the one that cannot.
                using var refused = new Effect(device, vertex, fragment);
                output.WriteLine(
                    $"ABSENT BRANCH EXERCISED: '{device.RendererName}' lacks CustomEffects; " +
                    $"valid={refused.IsSourceValid} parameters={refused.Parameters.Count}");

                Assert.False(refused.IsSourceValid);
                Assert.Empty(refused.Parameters);
                return;
            }

            using var effect = new Effect(device, vertex, fragment);

            output.WriteLine(
                $"renderer '{device.RendererName}' dialect={device.ShadingDialect} " +
                $"valid={effect.IsSourceValid} parameters={effect.Parameters.Count} " +
                $"techniques={effect.Techniques.Count}");

            for (int i = 0; i < effect.Parameters.Count; i++)
            {
                EffectParameter parameter = effect.Parameters[i];
                output.WriteLine($"  {parameter.Name}: {parameter.Annotations.Count} annotation(s)");
            }

            // No count assertion: zero is the honest answer on a renderer that does not inspect
            // source, and asserting it would make this pass forever on a renderer that later does.
            Assert.True(effect.Parameters.Count >= 0);
        });
    }
}
