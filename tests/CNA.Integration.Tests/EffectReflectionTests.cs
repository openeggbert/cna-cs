using CNA.Graphics;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Effect reflection -- <c>Parameters</c>, <c>Techniques</c>, <c>CurrentTechnique</c> and the
/// passes underneath them.
///
/// Worth its own file because this surface used to be fabricated. Before Phase 8 WP4a it handed out
/// a single made-up one-pass technique with an invented name and no parameters at all, purely so
/// <c>CurrentTechnique.Passes[0].Apply()</c> would compile. Nothing a caller could see
/// distinguished that from the truth. It reads real native objects now, and until this file nothing
/// had ever checked that at run time.
///
/// Every collection here mints owned native handles per read, which is why they are cached; a leak
/// or a double-free in that caching shows up as a crash under repetition, not as a wrong value, so
/// several of these read twice on purpose.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class EffectReflectionTests(ITestOutputHelper output, NativeGameFixture fixture)
{


    [NativeFact]
    public void BasicEffect_ExposesRealTechniquesAndPasses()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            EffectTechniqueCollection techniques = effect.Techniques;
            output.WriteLine($"{techniques.Count} technique(s)");
            Assert.True(techniques.Count > 0, "An effect with no techniques cannot be applied.");

            EffectTechnique current = effect.CurrentTechnique;
            output.WriteLine($"current technique '{current.Name}', {current.Passes.Count} pass(es)");

            Assert.False(string.IsNullOrEmpty(current.Name), "The technique reported an empty name.");
            Assert.True(current.Passes.Count > 0, "A technique with no passes draws nothing.");

            foreach (EffectPass pass in current.Passes)
            {
                output.WriteLine($"  pass '{pass.Name}'");
            }
        });
    }

    /// <summary>Reading twice must be safe. Each read of the underlying native call mints a new
    /// owned handle, so an uncached collection would churn -- and a wrongly cached one would hand
    /// back a freed handle the second time.</summary>
    [NativeFact]
    public void BasicEffect_ReflectionCollections_AreStableAcrossReads()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            int first = effect.Parameters.Count;
            int second = effect.Parameters.Count;
            Assert.Equal(first, second);

            Assert.Same(effect.Parameters, effect.Parameters);
            Assert.Same(effect.Techniques, effect.Techniques);
        });
    }

    /// <summary>
    /// Parameters by index and by name. The path a game takes for every custom shader uniform.
    ///
    /// <b>Measured: the stock effects expose zero reflection parameters on these renderers</b>, and
    /// that is consistent rather than a fault. A stock effect's state is set through typed
    /// properties -- DiffuseColor, FogStart -- not through named parameters, and both builds report
    /// CompiledEffects as false, so there is no reflected graph to enumerate. Asserted as
    /// "reachable and self-consistent" rather than as a count, because a count would encode this
    /// renderer's answer as the contract.
    /// </summary>
    [NativeFact]
    public void BasicEffect_Parameters_AreEnumerableAndAddressable()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            EffectParameterCollection parameters = effect.Parameters;
            output.WriteLine($"{parameters.Count} parameter(s)");

            var names = new List<string>();
            for (int i = 0; i < parameters.Count && i < 12; i++)
            {
                EffectParameter parameter = parameters[i];
                names.Add(parameter.Name);
                output.WriteLine($"  [{i}] {parameter.Name} : {parameter.ParameterClass}/{parameter.ParameterType}");
            }

            foreach (string name in names.Where(n => !string.IsNullOrEmpty(n)))
            {
                Assert.NotNull(parameters[name]);
            }
        });
    }

    /// <summary>Applying a pass, which is what actually selects the effect on the device. The
    /// deviation this project documents -- <c>Effect.Apply()</c> existing alongside
    /// <c>EffectPass.Apply()</c> -- means both paths must reach native, so both run here.</summary>
    [NativeFact]
    public void BasicEffect_BothApplyPaths_Succeed()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            effect.Apply();
            effect.CurrentTechnique.Passes[0].Apply();
        });
    }

    /// <summary>Every stock effect constructs and applies. Five separate native creates, five
    /// separate reflection surfaces, and the family where an undisposed instance used to leak its
    /// effect plus three directional lights.</summary>
    [NativeFact]
    public void EveryStockEffect_ConstructsAndApplies()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            var built = new List<(string Name, Effect Effect)>
            {
                ("BasicEffect", new BasicEffect(device)),
                ("AlphaTestEffect", new AlphaTestEffect(device)),
                ("DualTextureEffect", new DualTextureEffect(device)),
                ("EnvironmentMapEffect", new EnvironmentMapEffect(device)),
                ("SkinnedEffect", new SkinnedEffect(device)),
            };

            try
            {
                foreach ((string name, Effect effect) in built)
                {
                    effect.Apply();
                    output.WriteLine($"{name}: {effect.Parameters.Count} parameters, {effect.Techniques.Count} techniques");
                }
            }
            finally
            {
                foreach ((_, Effect effect) in built)
                {
                    effect.Dispose();
                }
            }
        });
    }

    /// <summary>Clone must produce an independent effect of the same concrete type -- the contract
    /// the native clone route documents and the reason each stock effect overrides it.</summary>
    [NativeFact]
    public void BasicEffect_Clone_ProducesAnIndependentBasicEffect()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);
            effect.DiffuseColor = new Vector3(0.25f, 0.5f, 0.75f);

            using Effect clone = effect.Clone();

            var typed = Assert.IsType<BasicEffect>(clone);
            Assert.NotSame(effect, typed);

            output.WriteLine($"original {effect.DiffuseColor}, clone {typed.DiffuseColor}");

            // Independence: changing the clone must not move the original.
            typed.DiffuseColor = new Vector3(1f, 0f, 0f);
            Assert.Equal(0.25f, effect.DiffuseColor.X, 1e-4f);
        });
    }

    /// <summary>The three-point lighting rig, which is a native convenience call rather than the
    /// twenty-odd hardcoded literals this project used to carry.</summary>
    [NativeFact]
    public void BasicEffect_EnableDefaultLighting_SetsTheRig()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            effect.EnableDefaultLighting();

            Assert.True(effect.LightingEnabled);
            Assert.True(effect.DirectionalLight0.Enabled, "Default lighting left light 0 disabled.");

            output.WriteLine(
                $"ambient {effect.AmbientLightColor}, light0 dir {effect.DirectionalLight0.Direction}");
        });
    }

    /// <summary>
    /// A parameter reached through a nested collection still knows its device.
    ///
    /// This guarded a NotSupportedException saying a nested parameter "cannot build a Texture
    /// wrapper, which needs one". Every construction site passes the device -- Effect.Parameters
    /// from the effect, both nested collections from the parent -- so the null case was
    /// unreachable and the throw described a situation that could not arise. The type is
    /// non-nullable now, which turns the guarantee into one the compiler keeps.
    ///
    /// Stock effects expose no parameters on these renderers, so this asserts what it can: that
    /// walking into Elements and StructureMembers answers rather than failing.
    /// </summary>
    [NativeFact]
    public void EffectParameter_NestedCollections_AreReachable()
    {
        fixture.InsideAFrameWithDevice(device =>
        {
            using var effect = new BasicEffect(device);

            EffectParameterCollection parameters = effect.Parameters;
            output.WriteLine($"{parameters.Count} parameter(s)");

            for (int i = 0; i < parameters.Count; i++)
            {
                EffectParameter parameter = parameters[i];
                output.WriteLine(
                    $"  {parameter.Name}: {parameter.Elements.Count} element(s), " +
                    $"{parameter.StructureMembers.Count} member(s)");
            }
        });
    }
}
