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
