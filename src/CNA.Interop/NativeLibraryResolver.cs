using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CNA.Interop;

/// <summary>
/// Resolves the binding's logical <c>cna-native</c> import to an explicitly configured CNA C API
/// library or to a native asset installed beside the application. It deliberately does not probe
/// the source tree or the process-wide loader path: package consumers must not succeed because an
/// unrelated developer or system installation happens to be visible.
/// </summary>
public static class NativeLibraryResolver
{
    /// <summary>Absolute path to one selected CNA C API library. Highest precedence.</summary>
    public const string PathVariable = "CNA_NATIVE_LIBRARY";

    /// <summary>Absolute directory containing exactly one recognized CNA C API library.</summary>
    public const string DirectoryVariable = "CNA_NATIVE_DIR";

    /// <summary>Set to <c>1</c> to include low-level loader exception details in diagnostics.</summary>
    public const string DiagnosticsVariable = "CNA_NATIVE_DIAGNOSTICS";

    private static readonly string[] CandidateNames = ["cna-native", "cna_c_api"];
    private static readonly string[] RequiredSymbols =
    [
        "cna_get_abi_version",
        "cna_error_get_last_message_size",
        "cna_game_create",
        "cna_game_destroy",
    ];

    private static int _registered;
    private static string? _loadedLibraryPath;
    private static string? _resolutionSource;
    private static uint? _detectedAbiVersion;

    /// <summary>The concrete library selected by the resolver, once loaded.</summary>
    public static string? LoadedLibraryPath => Volatile.Read(ref _loadedLibraryPath);

    /// <summary>The configuration route which selected the loaded library.</summary>
    public static string? ResolutionSource => Volatile.Read(ref _resolutionSource);

    /// <summary>The ABI reported by the selected library, when its version export was readable.</summary>
    public static uint? DetectedAbiVersion => _detectedAbiVersion;

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize() => Register();

    /// <summary>Registers the assembly-local resolver exactly once.</summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
    }

    /// <summary>Formats the selected-library context for a higher-level ABI diagnostic.</summary>
    internal static string DescribeSelection(uint expectedAbiVersion)
    {
        string detected = DetectedAbiVersion is uint version ? FormatVersion(version) : "unreadable";
        return $"Selected library: {LoadedLibraryPath ?? "unknown"} ({ResolutionSource ?? "unknown source"}). " +
               $"Detected ABI: {detected}; expected ABI: {FormatVersion(expectedAbiVersion)}; " +
               $"platform/RID: {RuntimeInformation.OSDescription} / {RuntimeInformation.RuntimeIdentifier}.";
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        _ = assembly;
        _ = searchPath;
        if (!string.Equals(libraryName, "cna-native", StringComparison.Ordinal))
        {
            return nint.Zero;
        }

        (string source, IReadOnlyList<string> paths) = SelectCandidates();
        if (paths.Count == 0)
        {
            throw new DllNotFoundException(BuildNotFoundMessage(source));
        }

        if (paths.Count > 1)
        {
            throw new DllNotFoundException(
                $"Conflicting CNA native libraries were found for {source}: {string.Join(", ", paths)}. " +
                "Keep exactly one recognized library, or set CNA_NATIVE_LIBRARY to the intended absolute path. " +
                PlatformSummary());
        }

        string path = paths[0];
        nint handle;
        try
        {
            handle = NativeLibrary.Load(path);
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or FileLoadException)
        {
            string category = exception is BadImageFormatException || LooksLikeArchitectureFailure(exception.Message)
                ? "The library has the wrong architecture or binary format"
                : "The library exists but the operating-system loader could not load it";
            throw new DllNotFoundException(
                $"{category}: {path}. {PlatformSummary()} " +
                "Use an ABI-matched CNA build for this RID and ensure its native dependencies are present." +
                Detailed(exception), exception);
        }

        try
        {
            if (!NativeLibrary.TryGetExport(handle, "cna_get_abi_version", out nint versionSymbol))
            {
                throw MissingSymbol(path, source, "cna_get_abi_version", detectedAbi: null);
            }

            uint abi = Marshal.GetDelegateForFunctionPointer<GetAbiVersion>(versionSymbol)();
            _detectedAbiVersion = abi;
            foreach (string symbol in RequiredSymbols)
            {
                if (!NativeLibrary.TryGetExport(handle, symbol, out _))
                {
                    throw MissingSymbol(path, source, symbol, abi);
                }
            }

            Volatile.Write(ref _loadedLibraryPath, path);
            Volatile.Write(ref _resolutionSource, source);
            return handle;
        }
        catch
        {
            NativeLibrary.Free(handle);
            throw;
        }
    }

    private static (string Source, IReadOnlyList<string> Paths) SelectCandidates()
    {
        string? explicitPath = Environment.GetEnvironmentVariable(PathVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (!Path.IsPathFullyQualified(explicitPath))
            {
                throw new DllNotFoundException(
                    $"{PathVariable} must be an absolute file path, but was '{explicitPath}'. {PlatformSummary()}");
            }

            string fullPath = Path.GetFullPath(explicitPath);
            if (!File.Exists(fullPath))
            {
                throw new DllNotFoundException(
                    $"{PathVariable} selected '{fullPath}', but that file does not exist. " +
                    $"Correct or clear {PathVariable}; no fallback is attempted when it is set. {PlatformSummary()}");
            }

            return ($"explicit {PathVariable}", [fullPath]);
        }

        string? explicitDirectory = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (!string.IsNullOrWhiteSpace(explicitDirectory))
        {
            if (!Path.IsPathFullyQualified(explicitDirectory))
            {
                throw new DllNotFoundException(
                    $"{DirectoryVariable} must be an absolute directory path, but was '{explicitDirectory}'. {PlatformSummary()}");
            }

            string fullDirectory = Path.GetFullPath(explicitDirectory);
            if (!Directory.Exists(fullDirectory))
            {
                throw new DllNotFoundException(
                    $"{DirectoryVariable} selected '{fullDirectory}', but that directory does not exist. " +
                    $"Correct or clear {DirectoryVariable}; no fallback is attempted when it is set. {PlatformSummary()}");
            }

            IReadOnlyList<string> configured = FindLibraries([fullDirectory]);
            if (configured.Count == 0)
            {
                throw new DllNotFoundException(
                    $"{DirectoryVariable} selected '{fullDirectory}', but it contains no recognized CNA library " +
                    $"({string.Join(", ", FileNames())}). No fallback is attempted when it is set. {PlatformSummary()}");
            }

            return ($"explicit {DirectoryVariable}", configured);
        }

        List<string> applicationDirectories = [];
        AddUniqueDirectory(applicationDirectories, AppContext.BaseDirectory);
        AddUniqueDirectory(applicationDirectories, Path.GetDirectoryName(typeof(Native).Assembly.Location));
        AddUniqueDirectory(applicationDirectories, Path.Combine(
            AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native"));
        return ("application/package native assets", FindLibraries(applicationDirectories));
    }

    private static IReadOnlyList<string> FindLibraries(IEnumerable<string> directories)
    {
        HashSet<string> paths = new(PathComparer());
        foreach (string directory in directories.Where(Directory.Exists))
        {
            foreach (string fileName in FileNames())
            {
                string path = Path.GetFullPath(Path.Combine(directory, fileName));
                if (File.Exists(path))
                {
                    paths.Add(path);
                }
            }
        }

        return paths.Order(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> FileNames()
    {
        foreach (string name in CandidateNames)
        {
            if (OperatingSystem.IsWindows())
            {
                yield return name + ".dll";
            }
            else if (OperatingSystem.IsMacOS())
            {
                yield return "lib" + name + ".dylib";
                yield return name + ".dylib";
            }
            else
            {
                yield return "lib" + name + ".so";
                yield return name + ".so";
            }
        }
    }

    private static void AddUniqueDirectory(List<string> directories, string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        string fullPath = Path.GetFullPath(directory);
        if (!directories.Contains(fullPath, PathComparer()))
        {
            directories.Add(fullPath);
        }
    }

    private static DllNotFoundException MissingSymbol(string path, string source, string symbol, uint? detectedAbi)
    {
        string abi = detectedAbi is uint version ? FormatVersion(version) : "unreadable";
        return new DllNotFoundException(
            $"The CNA library '{path}' selected through {source} loaded, but required symbol '{symbol}' is missing. " +
            $"Detected ABI: {abi}. {PlatformSummary()} Replace it with the ABI-matched CNA C API library; " +
            "do not rename an unrelated CNA library to a recognized filename.");
    }

    private static string BuildNotFoundMessage(string source)
    {
        StringBuilder message = new();
        message.Append("No CNA C API native library was found in the application/package native locations. ");
        message.Append(PlatformSummary());
        message.Append(' ');
        message.Append($"Supply the qualified RID native package, or set {PathVariable} to an ABI-matched absolute file path, ");
        message.Append($"or set {DirectoryVariable} to its absolute directory. Resolution source: {source}.");
        return message.ToString();
    }

    private static string PlatformSummary() =>
        $"Platform/RID: {RuntimeInformation.OSDescription} / {RuntimeInformation.RuntimeIdentifier} ({RuntimeInformation.ProcessArchitecture}).";

    private static bool LooksLikeArchitectureFailure(string message) =>
        message.Contains("wrong ELF class", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("invalid ELF", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("bad CPU type", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("not a valid Win32", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("incorrect format", StringComparison.OrdinalIgnoreCase);

    private static string Detailed(Exception exception) =>
        string.Equals(Environment.GetEnvironmentVariable(DiagnosticsVariable), "1", StringComparison.Ordinal)
            ? $" Loader detail ({exception.GetType().Name}): {exception.Message}"
            : string.Empty;

    private static StringComparer PathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static string FormatVersion(uint encoded) =>
        $"{(encoded >> 16) & 0xFFFF}.{(encoded >> 8) & 0xFF}.{encoded & 0xFF}";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GetAbiVersion();
}
