using System.Text.Json;

namespace CNA.ApiCompat;

internal sealed class CommandLine
{
    public string? TargetPath { get; private set; }
    public string? ReferenceDirectory { get; private set; }
    public List<string> ReferencePaths { get; } = [];
    public string? ProfilePath { get; private set; }
    public string? AllowlistPath { get; private set; }
    public List<string> NamespacePrefixes { get; } = [];
    public string Format { get; private set; } = "text";
    public bool ReportOnly { get; private set; }
    public bool LeakOnly { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CommandLine Parse(string[] args)
    {
        var options = new CommandLine();
        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            switch (argument)
            {
                case "--target":
                    options.TargetPath = ReadValue(args, ref index, argument);
                    break;
                case "--reference":
                    options.ReferencePaths.Add(ReadValue(args, ref index, argument));
                    break;
                case "--reference-dir":
                    options.ReferenceDirectory = ReadValue(args, ref index, argument);
                    break;
                case "--profile":
                    options.ProfilePath = ReadValue(args, ref index, argument);
                    break;
                case "--allowlist":
                    options.AllowlistPath = ReadValue(args, ref index, argument);
                    break;
                case "--namespace":
                    options.NamespacePrefixes.Add(ReadValue(args, ref index, argument));
                    break;
                case "--format":
                    options.Format = ReadValue(args, ref index, argument);
                    break;
                case "--report-only":
                    options.ReportOnly = true;
                    break;
                case "--leak-only":
                    options.LeakOnly = true;
                    break;
                case "--help" or "-h":
                    options.ShowHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'. Use --help for usage.");
            }
        }

        if (options.Format is not ("text" or "json" or "github"))
        {
            throw new ArgumentException("--format must be text, json, or github.");
        }

        return options;
    }

    public ResolvedInputs Resolve()
    {
        string repositoryRoot = FindRepositoryRoot();
        string target = ResolveFile(
            TargetPath ?? Environment.GetEnvironmentVariable("CNA_XNACOMPAT_ASSEMBLY") ??
            Path.Combine(repositoryRoot, "src", "CNA.XnaCompat", "bin", "Release", "net8.0", "CNA.XnaCompat.dll"),
            "target assembly");

        CompatibilityProfile profile = new()
        {
            Name = "command-line references",
            NamespacePrefixes = ["Microsoft.Xna.Framework"],
        };
        var references = new List<string>();

        if (!LeakOnly)
        {
            string profilePath = ProfilePath ??
                Path.Combine(repositoryRoot, "tools", "api-compat", "profiles", "xna40-windows-runtime.json");
            if (File.Exists(profilePath))
            {
                profile = JsonSerializer.Deserialize<CompatibilityProfile>(
                    File.ReadAllText(profilePath),
                    JsonOptions()) ?? throw new InvalidDataException($"Profile '{profilePath}' is empty.");
            }
            else if (ProfilePath is not null)
            {
                throw new FileNotFoundException($"Compatibility profile '{profilePath}' does not exist.", profilePath);
            }

            references.AddRange(ReferencePaths.Select(path => ResolveFile(path, "reference assembly")));
            string? referenceDirectory = ReferenceDirectory ?? Environment.GetEnvironmentVariable("XNA_REFERENCE_PATH");
            if (references.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(referenceDirectory))
                {
                    throw new InvalidOperationException(
                        "No XNA reference assemblies were configured. Pass --reference-dir, repeat --reference, " +
                        "or set XNA_REFERENCE_PATH.");
                }

                string directory = Path.GetFullPath(referenceDirectory);
                if (!Directory.Exists(directory))
                {
                    throw new DirectoryNotFoundException($"XNA reference directory '{directory}' does not exist.");
                }

                if (profile.ReferenceAssemblies.Count == 0)
                {
                    throw new InvalidDataException("The selected profile has no referenceAssemblies entries.");
                }

                references.AddRange(profile.ReferenceAssemblies.Select(fileName =>
                    ResolveFile(Path.Combine(directory, fileName), $"profile reference assembly '{fileName}'")));
            }
        }

        IReadOnlyList<string> namespacePrefixes = NamespacePrefixes.Count > 0
            ? NamespacePrefixes
            : profile.NamespacePrefixes.Count > 0
                ? profile.NamespacePrefixes
                : ["Microsoft.Xna.Framework"];
        string? allowlist = AllowlistPath;
        if (allowlist is null)
        {
            string candidate = Path.Combine(repositoryRoot, "tools", "api-compat", "api-compat.allowlist.json");
            allowlist = File.Exists(candidate) ? candidate : null;
        }

        return new ResolvedInputs(
            target,
            references,
            namespacePrefixes,
            allowlist is null ? null : ResolveFile(allowlist, "allowlist"),
            profile,
            repositoryRoot);
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (++index >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        return args[index];
    }

    private static string ResolveFile(string path, string description)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Configured {description} '{fullPath}' does not exist.", fullPath);
        }

        return fullPath;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CNA.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find CNA.sln above the current directory. Run from the repository or pass explicit paths.");
    }
}

internal sealed record ResolvedInputs(
    string TargetPath,
    IReadOnlyList<string> ReferencePaths,
    IReadOnlyList<string> NamespacePrefixes,
    string? AllowlistPath,
    CompatibilityProfile Profile,
    string RepositoryRoot);
