using CNA;
using CNA.Graphics;
using CNA.Interop;
using Xunit;

namespace CNA.Integration.Tests;

/// <summary>
/// Decides once, for the whole run, whether the real CNA C ABI library is loadable -- and records
/// *why* when it is not, so a skipped run says something more useful than "skipped".
///
/// These tests skip rather than fail when the library is absent, because it is not part of this
/// repository and not every checkout will have built it. A skip that explains itself is honest; a
/// green run that silently tested nothing is not, and that is precisely the state this project was
/// in until now: 701 passing tests, none of which loaded the library.
/// </summary>
public static class CnaNativeProbe
{
    private static readonly Lazy<string?> Failure = new(Detect);

    /// <summary>Null when the library loaded, otherwise the reason to show in the skip.</summary>
    public static string? SkipReason => Failure.Value;

    /// <summary>The ABI version the loaded library reports, for tests that want to log it.</summary>
    public static uint NativeVersion { get; private set; }

    /// <summary>
    /// Whether the fixture's live device has a capability, reporting the answer when it does not.
    ///
    /// The check has to happen in the test body rather than in a <c>Fact</c> attribute constructor:
    /// xUnit builds attributes during discovery, and creating a probe game there mutates CNA's
    /// process-global platform state before the first test runs.
    ///
    /// <b>It used to throw <c>SkipException.ForSkip</c>, which this runner reports as a failure.</b>
    /// Measured, not assumed: a probe test throwing one is reported <c>[FAIL]</c> with the raw
    /// marker <c>$XunitDynamicSkip$</c> as its message. Twenty call sites and two inline throws were
    /// therefore latent false failures -- each one would report a defect the first time it met a
    /// renderer it was written to tolerate, and "this renderer is 2D-only by design" would have read
    /// as "the binding is broken".
    ///
    /// So the caller returns instead, and the reason is printed. <b>That makes such a test a silent
    /// pass</b>, which is the honest cost of this runner and is why the reason goes to the output
    /// rather than nowhere. The better end state is the one the mouse-cursor test reached -- assert
    /// what must happen when the capability is absent, so both branches carry an assertion -- and
    /// that is per-test work recorded in plan.md A7 rather than something this helper can do.
    /// </summary>
    public static bool HasCapability(
        CNA.Graphics.GraphicsDevice device,
        CNA.Graphics.GraphicsCapability capability,
        Xunit.Abstractions.ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (device.SupportsCapability(capability))
        {
            return true;
        }

        output?.WriteLine(
            $"NOT EXERCISED: renderer '{device.RendererName}' does not report {capability}, " +
            "and this test needs it.");
        return false;
    }

    /// <summary>
    /// The capability gate that <b>asserts</b> on the absent branch instead of returning silently.
    ///
    /// <see cref="HasCapability"/> leaves a test proving nothing on the one renderer where the
    /// binding's refusal behaviour actually matters. This closes that: when the capability is
    /// present it returns <see langword="true"/> and the test proceeds; when it is absent it runs
    /// <paramref name="refused"/> and requires <c>NotSupported</c>, so both branches carry an
    /// assertion and neither is a silent pass.
    ///
    /// <b><c>NotSupported</c> is measured, not assumed.</b> The reason those branches stayed
    /// unasserted was a real uncertainty: <c>IGraphicsRenderer::HandleUnsupported3DCall</c> throws a
    /// bare <c>std::runtime_error</c>, which the C API's exception barrier maps to
    /// <c>CNA_RESULT_INTERNAL</c>, so a 2D-only renderer might plausibly have refused with either
    /// code. A <c>SDL_RENDERER</c> build settles it: every 3D operation the C API offers -- vertex
    /// and index buffers, dynamic buffers, occlusion queries, <c>Texture3D</c>, all three draw
    /// families, cube render targets and cube-face storage -- is refused with <c>NotSupported</c>
    /// and a specific message. None reaches the unguarded path, because the C API checks the
    /// capability before the renderer is asked. That is a fact about upstream and is why this helper
    /// can require one code rather than tolerate two.
    ///
    /// <paramref name="refused"/> should be the *smallest* operation the test depends on, not the
    /// whole test body: the point is to pin which call the renderer refuses.
    /// </summary>
    public static bool HasCapabilityOrRefuses(
        CNA.Graphics.GraphicsDevice device,
        CNA.Graphics.GraphicsCapability capability,
        string what,
        Action refused,
        Xunit.Abstractions.ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(refused);

        if (device.SupportsCapability(capability))
        {
            return true;
        }

        output?.WriteLine(
            $"renderer '{device.RendererName}' does not report {capability}; asserting the refusal.");
        AssertRefusedAsNotSupported(what, refused, output!);
        return false;
    }

    /// <summary>
    /// Asserts that a native operation is refused with <c>NOT_SUPPORTED</c>, which is what makes an
    /// absent-capability branch evidence instead of a silent pass.
    ///
    /// <see cref="HasCapability"/> lets a test skip the branch it cannot run; this is the other
    /// half. A test that returns early on an absent capability can never fail there, so on a
    /// renderer lacking it the test proves nothing at all -- and that is precisely the renderer
    /// where the binding's refusal behaviour matters, because it is the only place the refusal
    /// happens.
    ///
    /// The result string is checked rather than only the exception type: every native failure
    /// arrives as <see cref="CnaException"/>, so accepting any of them would accept an
    /// <c>INVALID_ARGUMENT</c> from a test that built its arguments wrongly.
    /// </summary>
    public static void AssertRefusedAsNotSupported(
        string what,
        Action operation,
        Xunit.Abstractions.ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(operation);

        CnaException failure = Assert.Throws<CnaException>(operation);
        Assert.Equal("NotSupported", failure.NativeResult);
        output?.WriteLine($"ABSENT BRANCH EXERCISED: {what} refused with NotSupported -- {failure.Message}");
    }

    private static readonly Dictionary<string, bool> ReadbackByRenderer = new(StringComparer.Ordinal);
    private static readonly object ReadbackLock = new();

    /// <summary>
    /// Whether this renderer can read a render target's colour attachment back to the CPU.
    ///
    /// <b>There is no capability identity for this.</b> <c>CNA_GRAPHICS_CAPABILITY_*</c> names
    /// nineteen things and readback is not one of them, so unlike <see cref="HasCapability"/> this
    /// cannot be asked -- it has to be measured. HEADLESS reports every capability except
    /// <c>Texture3D</c>, <c>AdditiveBlending</c> and <c>MultiStreamVertexInput</c>, and still
    /// answers <c>Texture2D::GetData: this graphics renderer cannot read a render target's colour
    /// attachment back to the CPU</c>, which failed five pixel-evidence tests as though the binding
    /// were broken.
    ///
    /// The measurement catches the exception, which a *test* must never do; a probe whose entire
    /// purpose is to determine one fact is the exception, and it is why this lives here rather than
    /// in each test. It runs once per renderer name and the answer then selects which assertion the
    /// caller makes, so both branches still assert.
    /// </summary>
    public static bool SupportsRenderTargetReadback(
        CNA.Graphics.GraphicsDevice device,
        Xunit.Abstractions.ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(device);

        string renderer = device.RendererName;
        lock (ReadbackLock)
        {
            if (ReadbackByRenderer.TryGetValue(renderer, out bool known))
            {
                return known;
            }

            bool supported;
            try
            {
                using var probe = new RenderTarget2D(device, 1, 1);
                device.SetRenderTarget(probe);
                device.Clear(new Color(1, 2, 3, 255));
                device.SetRenderTarget(null);

                var pixel = new Color[1];
                probe.GetData(pixel);
                supported = true;
            }
            catch (CnaException failure) when (failure.NativeResult == "NotSupported")
            {
                output?.WriteLine(
                    $"MEASURED: renderer '{renderer}' cannot read a render target back -- {failure.Message}");
                supported = false;
            }

            ReadbackByRenderer[renderer] = supported;
            return supported;
        }
    }

    /// <summary>
    /// The gate a pixel-evidence test opens with: <see langword="true"/> when the renderer can read
    /// a render target back, and otherwise <see langword="false"/> **after asserting that it refuses
    /// to**.
    ///
    /// That asymmetry with <see cref="HasCapability"/> is the point. <c>HasCapability</c> lets a
    /// test return early and prove nothing, which is the honest cost of this runner for a branch
    /// nobody can exercise. This branch *can* be exercised -- HEADLESS refuses readback -- so it
    /// carries an assertion instead, and a binding that started swallowing the refusal, or reporting
    /// it as some other result, fails here rather than passing quietly.
    /// </summary>
    public static bool RequireRenderTargetReadback(
        CNA.Graphics.GraphicsDevice device,
        Xunit.Abstractions.ITestOutputHelper output)
    {
        if (SupportsRenderTargetReadback(device, output))
        {
            return true;
        }

        AssertRefusedAsNotSupported(
            "reading a render target back to the CPU",
            () =>
            {
                using var target = new RenderTarget2D(device, 4, 4);
                target.GetData(new Color[16]);
            },
            output);
        return false;
    }

    private static readonly Dictionary<string, bool> CubeFaceByRenderer = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether this renderer stores a cube-map face transfer. Measured for the same reason
    /// <see cref="SupportsRenderTargetReadback"/> is: there is no capability identity for it, and
    /// <c>ThreeD</c> is not it -- HEADLESS reports <c>ThreeD</c> and answers
    /// <c>TextureCube::SetData: this graphics renderer did not store the complete requested cube
    /// face region</c>.
    /// </summary>
    public static bool SupportsCubeFaceStorage(
        CNA.Graphics.GraphicsDevice device,
        Xunit.Abstractions.ITestOutputHelper? output = null)
    {
        ArgumentNullException.ThrowIfNull(device);

        string renderer = device.RendererName;
        lock (ReadbackLock)
        {
            if (CubeFaceByRenderer.TryGetValue(renderer, out bool known))
            {
                return known;
            }

            bool supported;
            try
            {
                using var probe = new TextureCube(device, 2);
                probe.SetData(CubeMapFace.PositiveX, new Color[4]);
                supported = true;
            }
            catch (CnaException failure) when (failure.NativeResult == "NotSupported")
            {
                output?.WriteLine(
                    $"MEASURED: renderer '{renderer}' does not store cube faces -- {failure.Message}");
                supported = false;
            }

            CubeFaceByRenderer[renderer] = supported;
            return supported;
        }
    }

    private static string? Detect()
    {
        try
        {
            NativeVersion = CnaAbi.NativeVersion;
            return NativeVersion == 0
                ? "cna_get_abi_version returned 0, which is not a valid ABI version."
                : null;
        }
        catch (DllNotFoundException ex)
        {
            return $"CNA native library not loadable. Set {NativeLibraryResolver.PathVariable} to its " +
                   $"full path or {NativeLibraryResolver.DirectoryVariable} to its directory. " +
                   $"Loader said: {ex.Message}";
        }
        catch (EntryPointNotFoundException ex)
        {
            // Worth distinguishing from a missing library: this one means the library loaded but is
            // a different ABI generation. That is a real finding, so name the symbol.
            return $"Library loaded but an expected symbol is missing -- likely an ABI mismatch: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Loading the CNA native library failed unexpectedly: {ex.GetType().Name}: {ex.Message}";
        }
    }
}

/// <summary>A <see cref="FactAttribute"/> that skips itself when the native library is missing,
/// carrying the loader's own reason.</summary>
public sealed class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute()
    {
        if (CnaNativeProbe.SkipReason is { } reason)
        {
            Skip = reason;
        }
    }

    /// <summary>
    /// Setting <see cref="FactAttribute.Skip"/> directly on the attribute skips the test even when
    /// the library <em>is</em> present -- for a test that asserts correct behaviour which a known
    /// upstream defect currently prevents.
    ///
    /// Explicit rather than incidental: the alternative is rewriting such a test to assert the
    /// broken behaviour, which makes it pass forever and stops it being a question. A skip carries
    /// its reason into every run, and deleting the reason is the verification.
    /// </summary>
    public NativeFactAttribute(string skipReason)
    {
        Skip = CnaNativeProbe.SkipReason ?? skipReason;
    }
}

/// <summary>
/// Marks a native test that performs a live <see cref="CnaNativeProbe.HasCapability"/> check
/// for a 3D pipeline in its body.
///
/// For tests whose subject genuinely needs one -- a vertex or index buffer cannot even be created
/// on SDL_RENDERER. Without this they failed there with <c>NotSupported</c>, which reads as a
/// broken binding and is nothing of the sort: that renderer is 2D-only by design and behaving
/// exactly as documented. "The renderer cannot" and "the binding is broken" are different results
/// and were being reported identically.
/// </summary>
public sealed class Native3DFactAttribute() : NativeFactRequiringAttribute(CNA.Graphics.GraphicsCapability.ThreeD);

/// <summary>
/// Marks a native test that performs a live <see cref="CnaNativeProbe.HasCapability"/> check
/// for a named capability in its body.
///
/// General rather than one attribute per capability, because the list keeps growing as tests reach
/// further: SDL_RENDERER has no 3D pipeline, and SOFTWARE has 3D but no volume-texture storage. A
/// test that names the capability it needs skips with a reason that says which renderer lacked it,
/// instead of failing with a NotSupported that reads like a broken binding.
/// </summary>
public class NativeFactRequiringAttribute : FactAttribute
{
    public NativeFactRequiringAttribute(CNA.Graphics.GraphicsCapability capability)
    {
        _ = capability;
        if (CnaNativeProbe.SkipReason is { } reason)
        {
            Skip = reason;
        }

        // Do not create a game while xUnit is discovering attributes. CNA has a single active-game
        // slot and some renderers cannot tear down and reacquire their platform video subsystem;
        // the old discovery-time probe changed native process state before the first test ran.
        // Capability-specific tests perform their check against the fixture's live device instead.
    }
}
