using CNA.Graphics;
using CNA.Graphics.Experimental;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D3's third vertical slice: the bloom and tonemap post-process passes.
///
/// <b>Why this family and not a bigger one.</b> <c>engine_layer.h</c> has larger families -- PBR
/// materials at 63 routes, clustered lighting at 60 -- and every one of them has the same problem
/// the post-process chain slice already ran into: a shader's output can only be checked against a
/// reimplementation of the shader, which tests the reimplementation. Bloom and tonemap are the two
/// that expose their own arithmetic as <b>pure, deviceless functions</b>, so the curve can be asked
/// for rather than inferred from pixels. That is the strongest evidence available anywhere in this
/// header, and it is why these two were taken next.
///
/// It also repairs a hole in the previous slice. A blit is the identity, which is what made the
/// chain's pixel assertions vacuous until the pooled-target count was found to distinguish them; a
/// tonemap is not, and <see cref="TonemapSettings.TonemapChannel"/> says exactly what it should do.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class HdrPassTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    /// <summary>
    /// The extraction curve, which is not the obvious one.
    ///
    /// A reader would expect <c>max(value - threshold, 0)</c>. Measured, it is a soft knee: exactly
    /// at the threshold the result is a quarter rather than zero, and well above it the value passes
    /// through unchanged rather than arriving reduced by the threshold. Both differences are
    /// asserted, because each on its own is satisfied by a wrong curve -- the subtractive version
    /// gives 0 at the knee and 1 at twice the threshold, and this gives 0.25 and 2.
    ///
    /// <b>Deviceless is not unconditional.</b> These routes take no device and still answer
    /// <c>NotSupported</c> on a build without the engine layer -- measured, after the first version
    /// of this test asserted otherwise and failed on HEADLESS. The arithmetic is pure; its presence
    /// in the binary is not, and the absent branch is asserted rather than skipped.
    /// </summary>
    [NativeFact]
    public void BloomExtraction_HasASoftKneeRatherThanASubtraction()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPureRoutesRefusedWithoutTheEngineLayer();
                return;
            }

            const float Threshold = 1f;

            Assert.Equal(0f, BloomSettings.ExtractChannel(0f, Threshold));
            Assert.Equal(0f, BloomSettings.ExtractChannel(0.5f, Threshold));

            // The knee. A subtraction would answer 0 here.
            float atThreshold = BloomSettings.ExtractChannel(Threshold, Threshold);
            Assert.True(atThreshold > 0f, "At the threshold a soft knee contributes; a subtraction does not.");
            Assert.Equal(0.25f, atThreshold, 5);

            // Well above it, unchanged. A subtraction would answer 1 and 3.
            Assert.Equal(2f, BloomSettings.ExtractChannel(2f, Threshold), 5);
            Assert.Equal(4f, BloomSettings.ExtractChannel(4f, Threshold), 5);

            // And the threshold is honoured rather than ignored: raising it darkens the same input.
            Assert.True(
                BloomSettings.ExtractChannel(2f, 4f) < BloomSettings.ExtractChannel(2f, 1f),
                "A higher threshold must extract less from the same value.");

            output.WriteLine($"extract(1,1)={atThreshold} extract(2,1)={BloomSettings.ExtractChannel(2f, 1f)}");
        });
    }

    /// <summary>Quality tiers give strictly more blur iterations, and the exact counts CNA
    /// chose.</summary>
    [NativeFact]
    public void BloomIterations_RiseStrictlyWithQuality()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPureRoutesRefusedWithoutTheEngineLayer();
                return;
            }

            int low = BloomSettings.IterationsForQuality(RenderQuality.Low);
            int medium = BloomSettings.IterationsForQuality(RenderQuality.Medium);
            int high = BloomSettings.IterationsForQuality(RenderQuality.High);
            int ultra = BloomSettings.IterationsForQuality(RenderQuality.Ultra);

            output.WriteLine($"iterations low={low} medium={medium} high={high} ultra={ultra}");

            // Strictly increasing, so a binding that passed a constant quality -- or that lost the
            // parameter entirely -- reports four equal numbers and fails.
            Assert.True(low < medium && medium < high && high < ultra);
            Assert.Equal([2, 3, 5, 7], new[] { low, medium, high, ultra });
        });
    }

    /// <summary>
    /// Every tonemapping mode is a different curve, and each parameter reaches the one it names.
    ///
    /// The distinctness assertion is what catches a binding that dropped the mode: five identical
    /// answers. The exposure/gamma pair is asserted asymmetrically on purpose -- they are adjacent
    /// floats in the signature, so swapping them is the easy mistake, and it survives any test that
    /// only varies one of them.
    /// </summary>
    [NativeFact]
    public void Tonemapping_IsADifferentCurvePerMode_AndUsesBothParameters()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPureRoutesRefusedWithoutTheEngineLayer();
                return;
            }

            var mapped = new List<float>();
            foreach (TonemappingMode mode in Enum.GetValues<TonemappingMode>())
            {
                float value = TonemapSettings.TonemapChannel(mode, 1f, 1f, 2.2f);
                output.WriteLine($"{mode}(1, exposure 1, gamma 2.2) = {value}");
                mapped.Add(value);
            }

            Assert.Equal(mapped.Count, mapped.Distinct().Count());

            // Brighter in is never darker out, for every curve.
            foreach (TonemappingMode mode in Enum.GetValues<TonemappingMode>())
            {
                Assert.True(
                    TonemapSettings.TonemapChannel(mode, 4f, 1f, 2.2f)
                        >= TonemapSettings.TonemapChannel(mode, 1f, 1f, 2.2f),
                    $"{mode} mapped a brighter value darker.");
            }

            // Exposure and gamma, each pinned to what it actually means rather than to "they
            // differ". The first version of this asserted only that swapping the two produced
            // different answers -- which is symmetric, and a binding that swapped them at the call
            // site passed it. Proven: the swap mutation was green until this was rewritten.
            //
            // With no curve in the way, the two are separable and their meanings are different
            // shapes, which is what makes these assertions asymmetric:
            //   exposure is a linear multiplier applied to the value,
            //   gamma is an encoding exponent applied as value^(1/gamma).
            // 0.1 is chosen so the two answers cannot coincide: at 0.25 with 2 and 2 they do.
            Assert.Equal(
                0.1f * 2.2f,
                TonemapSettings.TonemapChannel(TonemappingMode.None, 0.1f, 2.2f, 1f),
                4);
            Assert.Equal(
                MathF.Pow(0.1f, 1f / 2.2f),
                TonemapSettings.TonemapChannel(TonemappingMode.None, 0.1f, 1f, 2.2f),
                4);

            // And exposure runs before gamma, not after: (0.25 * 2) ^ (1/2), not 0.25 ^ (1/2) * 2.
            Assert.Equal(
                MathF.Pow(0.25f * 2f, 1f / 2f),
                TonemapSettings.TonemapChannel(TonemappingMode.None, 0.25f, 2f, 2f),
                4);

            // The result is clamped, and after exposure rather than before it.
            Assert.Equal(1f, TonemapSettings.TonemapChannel(TonemappingMode.None, 0.5f, 4f, 1f), 4);
        });
    }

    /// <summary><see cref="TonemappingMode.None"/> still clamps into range. Worth pinning because
    /// "None" reads as a pass-through, and a caller relying on that would send values above one to a
    /// display that cannot show them.</summary>
    [NativeFact]
    public void TonemappingModeNone_StillClampsIntoRange()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertPureRoutesRefusedWithoutTheEngineLayer();
                return;
            }

            float bright = TonemapSettings.TonemapChannel(TonemappingMode.None, 4f, 1f, 2.2f);
            output.WriteLine($"None(4) = {bright}");
            Assert.Equal(1f, bright, 5);
        });
    }

    /// <summary>
    /// Every setting round-trips through the native pass, and the defaults are what CNA chose.
    ///
    /// Each property is written with a value the default is not, so a getter wired to the wrong
    /// route -- or a setter that did nothing -- shows up as the default coming back.
    /// </summary>
    [NativeFact]
    public void BloomAndTonemapSettings_RoundTripThroughTheNativePass()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertHdrPassesRefusedWithoutTheEngineLayer(device);
                return;
            }

            using PostProcessPass bloom = PostProcessPass.CreateBloom(device);
            Assert.Equal("Bloom", bloom.Name);
            Assert.Equal(1f, bloom.Bloom.Threshold, 5);
            Assert.Equal(1f, bloom.Bloom.Intensity, 5);
            Assert.Equal(4, bloom.Bloom.Iterations);

            bloom.Bloom.Threshold = 0.75f;
            bloom.Bloom.Intensity = 2.5f;
            bloom.Bloom.Iterations = 6;

            Assert.Equal(0.75f, bloom.Bloom.Threshold, 5);
            Assert.Equal(2.5f, bloom.Bloom.Intensity, 5);
            Assert.Equal(6, bloom.Bloom.Iterations);

            // The view is a window, not a copy: a second view over the same pass sees the writes.
            Assert.Equal(0.75f, bloom.Bloom.Threshold, 5);

            using PostProcessPass tonemap = PostProcessPass.CreateTonemap(device);
            Assert.Equal("Tonemap", tonemap.Name);
            Assert.Equal(TonemappingMode.None, tonemap.Tonemap.Mode);
            Assert.Equal(1f, tonemap.Tonemap.Exposure, 5);
            Assert.Equal(2.2f, tonemap.Tonemap.Gamma, 5);
            Assert.False(tonemap.Tonemap.DebandEnabled);

            // Every mode, not one: the enum crosses the ABI as a numeric cast, and an off-by-one
            // there round-trips a single value perfectly.
            foreach (TonemappingMode mode in Enum.GetValues<TonemappingMode>())
            {
                tonemap.Tonemap.Mode = mode;
                Assert.Equal(mode, tonemap.Tonemap.Mode);
            }

            tonemap.Tonemap.Exposure = 1.75f;
            tonemap.Tonemap.Gamma = 1.8f;
            tonemap.Tonemap.DebandEnabled = true;
            tonemap.Tonemap.DebandStrength = 0.4f;

            Assert.Equal(1.75f, tonemap.Tonemap.Exposure, 5);
            Assert.Equal(1.8f, tonemap.Tonemap.Gamma, 5);
            Assert.True(tonemap.Tonemap.DebandEnabled);
            Assert.Equal(0.4f, tonemap.Tonemap.DebandStrength, 5);

            output.WriteLine("bloom and tonemap settings round-trip");
        });
    }

    /// <summary>
    /// Asking a pass for another pass's settings is refused by CNA, not by a check repeated here.
    ///
    /// This is why the settings are typed views rather than properties on
    /// <see cref="PostProcessPass"/>: native already answers <c>InvalidArgument</c>, so the managed
    /// side neither duplicates the check nor pretends every pass has a threshold.
    /// </summary>
    [NativeFact]
    public void SettingsOfTheWrongKind_AreRefusedByNative()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertHdrPassesRefusedWithoutTheEngineLayer(device);
                return;
            }

            using PostProcessPass tonemap = PostProcessPass.CreateTonemap(device);
            CnaException failure = Assert.Throws<CnaException>(() => _ = tonemap.Bloom.Threshold);
            Assert.Equal("InvalidArgument", failure.NativeResult);

            using PostProcessPass blit = PostProcessPass.CreateBlit(device);
            Assert.Equal("InvalidArgument", Assert.Throws<CnaException>(() => _ = blit.Tonemap.Mode).NativeResult);

            output.WriteLine($"cross-type settings refused: {failure.Message}");
        });
    }

    /// <summary>
    /// The passes are chain members like any other, including the owning add.
    ///
    /// <c>AddOwned</c> is where the previous slice found a double release, so a new pass kind goes
    /// through it deliberately: CNA consumes the handle whether or not the call succeeds, the
    /// wrapper goes inert, and disposing it afterwards must be safe.
    /// </summary>
    [NativeFact]
    public void HdrPasses_JoinAChainAndTransferOwnership()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            if (!GraphicsDevice.IsCnaEngineLayerAvailable())
            {
                AssertHdrPassesRefusedWithoutTheEngineLayer(device);
                return;
            }

            using var chain = new PostProcessChain(device);
            Assert.Equal(0, chain.PassCount);

            using PostProcessPass borrowedBloom = PostProcessPass.CreateBloom(device);
            chain.Add(borrowedBloom);
            Assert.Equal(1, chain.PassCount);

            PostProcessPass ownedTonemap = PostProcessPass.CreateTonemap(device);
            chain.AddOwned(ownedTonemap);
            Assert.Equal(2, chain.PassCount);

            // Surrendered, so this is the chain's to destroy. Disposing the inert wrapper is safe
            // and must not release the handle a second time.
            ownedTonemap.Dispose();
            Assert.Equal(2, chain.PassCount);

            // The borrowed one is still the caller's and is still readable.
            Assert.Equal(1f, borrowedBloom.Bloom.Threshold, 5);

            chain.Clear();
            Assert.Equal(0, chain.PassCount);
        });
    }

    /// <summary>
    /// What a build without the engine layer must do.
    ///
    /// Every engine-layer symbol resolves in every CNA build, so "the factory is there" proves
    /// nothing; a build that lacks the layer answers <c>NOT_SUPPORTED</c> at call time and that is
    /// what is asserted. Both factories, because a binding that gated one and not the other would
    /// pass a single-factory check.
    /// </summary>
    /// <summary>
    /// The absent branch for the deviceless routes.
    ///
    /// Worth asserting rather than skipping precisely because these take no device: "it needs no
    /// device, so it must always work" is the reasonable inference, it is wrong, and a test that
    /// returned quietly here would leave the inference standing.
    /// </summary>
    private static void AssertPureRoutesRefusedWithoutTheEngineLayer()
    {
        Assert.Equal(0, GraphicsDevice.CnaEngineLayerVersion());
        Assert.Equal(
            "NotSupported",
            Assert.Throws<CnaException>(() => BloomSettings.ExtractChannel(2f, 1f)).NativeResult);
        Assert.Equal(
            "NotSupported",
            Assert.Throws<CnaException>(() => BloomSettings.IterationsForQuality(RenderQuality.High)).NativeResult);
        Assert.Equal(
            "NotSupported",
            Assert.Throws<CnaException>(
                () => TonemapSettings.TonemapChannel(TonemappingMode.Aces, 1f, 1f, 2.2f)).NativeResult);
    }

    private static void AssertHdrPassesRefusedWithoutTheEngineLayer(GraphicsDevice device)
    {
        Assert.Equal(0, GraphicsDevice.CnaEngineLayerVersion());

        Assert.Equal(
            "NotSupported",
            Assert.Throws<CnaException>(() => PostProcessPass.CreateBloom(device).Dispose()).NativeResult);
        Assert.Equal(
            "NotSupported",
            Assert.Throws<CnaException>(() => PostProcessPass.CreateTonemap(device).Dispose()).NativeResult);
    }
}
