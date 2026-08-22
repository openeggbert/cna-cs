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

    /// <summary>Checks a capability on the fixture's already-live device. This must happen in the
    /// test body, not in a Fact attribute constructor: xUnit constructs attributes during
    /// discovery, and creating a probe game there mutates CNA's process-global platform state
    /// before the first test runs.</summary>
    public static void RequireCapability(
        CNA.Graphics.GraphicsDevice device,
        CNA.Graphics.GraphicsCapability capability)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (!device.SupportsCapability(capability))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                $"Renderer '{device.RendererName}' does not report {capability}, and this test needs it.");
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
/// Marks a native test that performs a live <see cref="CnaNativeProbe.RequireCapability"/> check
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
/// Marks a native test that performs a live <see cref="CnaNativeProbe.RequireCapability"/> check
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
