using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Maps the <c>cna-native</c> name every <c>[LibraryImport]</c> in <see cref="Native"/> uses onto
/// whatever the CNA C ABI shared library is actually called on this machine.
///
/// This exists because the two names do not match and never have: the binding asks for
/// <c>cna-native</c>, and <c>openeggbert/cna</c> builds <c>libcna_c_api.so</c>. The default
/// probing rules would look for <c>libcna-native.so</c>, <c>cna-native.so</c> and friends, find
/// none of them, and throw <see cref="DllNotFoundException"/> on the first call -- which is
/// exactly what <c>samples/HelloGame</c>'s README reported, and attributed to the native library
/// "not existing upstream yet". It does exist. It is called something else.
///
/// That is worth stating plainly, because it means the entire binding was unloadable while every
/// static check passed: 807 declarations verified against the headers, then verified again against
/// the shipped symbol table, all of them correct, and not one of them reachable.
/// </summary>
public static class NativeLibraryResolver
{
    /// <summary>Full path to the library, if the caller wants to be explicit. Highest priority,
    /// and the only thing that works if the library lives somewhere unusual.</summary>
    public const string PathVariable = "CNA_NATIVE_LIBRARY";

    /// <summary>A directory to search, for the common case of "I built the engine over there".
    /// Every known file name is tried inside it.</summary>
    public const string DirectoryVariable = "CNA_NATIVE_DIR";

    /// <summary>
    /// The names to try, most-specific first.
    ///
    /// <c>cna-native</c> stays first so a properly packaged binding -- one that ships
    /// <c>runtimes/&lt;rid&gt;/native/libcna-native.so</c>, per plan.md Phase 6 -- keeps working
    /// without this resolver mattering. <c>cna_c_api</c> is what the engine's CMake actually
    /// produces today.
    /// </summary>
    private static readonly string[] CandidateNames = ["cna-native", "cna_c_api", "cna"];

    private static int _registered;

    /// <summary>
    /// Registers the resolver on first use of this assembly, so nothing has to remember to call it.
    ///
    /// A <see cref="ModuleInitializerAttribute"/> rather than a static constructor on
    /// <see cref="Native"/>: <c>[LibraryImport]</c> generates its own partial methods that do not
    /// touch any field, so a static constructor is not guaranteed to have run before the first
    /// P/Invoke resolves.
    /// </summary>
    /// <remarks>
    /// CA2255 objects to module initializers in libraries, on the grounds that a consumer cannot
    /// see or control them. That is the right default and the wrong call here: the alternative is
    /// requiring every consumer to call <see cref="Register"/> before touching any CNA type, which
    /// is a rule nobody will remember and whose failure mode is a <see cref="DllNotFoundException"/>
    /// deep inside a constructor. Registering a <c>DllImportResolver</c> is the documented use for
    /// this attribute, it touches nothing but this assembly's own import resolution, and
    /// <see cref="Register"/> remains public for anyone who wants to be explicit.
    /// </remarks>
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => Register();

    /// <summary>Idempotent -- <see cref="NativeLibrary.SetDllImportResolver"/> throws if called
    /// twice for one assembly, and a test host can load this assembly more than once.</summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "cna-native")
        {
            return nint.Zero;
        }

        foreach (string candidate in Probe())
        {
            if (NativeLibrary.TryLoad(candidate, out nint handle))
            {
                return handle;
            }
        }

        // Zero hands control back to the default resolver, whose DllNotFoundException names
        // "cna-native" and lists the paths it tried -- a better message than anything invented
        // here, and it keeps the failure looking like a normal missing-library failure.
        return nint.Zero;
    }

    private static IEnumerable<string> Probe()
    {
        string? explicitPath = Environment.GetEnvironmentVariable(PathVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath;
        }

        // Bare names first: NativeLibrary.TryLoad applies the platform's own prefix/suffix rules
        // and searches the standard loader paths, which is what a system-installed engine needs.
        foreach (string name in CandidateNames)
        {
            yield return name;
        }

        foreach (string directory in SearchDirectories())
        {
            foreach (string name in CandidateNames)
            {
                foreach (string fileName in FileNames(name))
                {
                    yield return Path.Combine(directory, fileName);
                }
            }
        }
    }

    private static IEnumerable<string> SearchDirectories()
    {
        string? configured = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            yield return configured;
        }

        string? beside = Path.GetDirectoryName(typeof(Native).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(beside))
        {
            yield return beside;
        }

        yield return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> FileNames(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            yield return name + ".dll";
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return "lib" + name + ".dylib";
            yield return name + ".dylib";
            yield break;
        }

        yield return "lib" + name + ".so";
        yield return name + ".so";
    }
}
