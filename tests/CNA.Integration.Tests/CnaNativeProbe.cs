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
