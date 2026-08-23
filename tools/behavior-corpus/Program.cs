using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CNA.BehaviorCorpus;

internal static partial class Program
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false, true);

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "--help" or "-h")
            {
                PrintHelp();
                return args.Length == 0 ? 2 : 0;
            }

            string repositoryRoot = FindRepositoryRoot();
            string manifestPath = ReadOption(args, "--manifest") ??
                Path.Combine(repositoryRoot, "tests", "behavior-corpus-counts.json");
            CorpusManifest manifest = LoadManifest(manifestPath, repositoryRoot);

            return args[0] switch
            {
                "verify" => Verify(manifest),
                "get" => GetValue(manifest, args),
                "summary" => Summary(manifest, HasFlag(args, "--json")),
                "validate" => ValidateCommand(manifest, args),
                "combine" => Combine(manifest, args),
                "compare" => Compare(args),
                "docs" => GenerateDocumentation(manifest, args),
                _ => throw new ArgumentException($"Unknown command '{args[0]}'. Use --help for usage."),
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or JsonException)
        {
            Console.Error.WriteLine($"behavior-corpus: {exception.Message}");
            return 2;
        }
    }

    private static int Verify(CorpusManifest manifest)
    {
        Console.WriteLine(
            $"behavior-corpus: manifest valid; categories={manifest.Categories.Count}, " +
            $"probes={manifest.Probes.Count}, observations={manifest.CombinedExpectedObservationCount}.");
        return 0;
    }

    private static int GetValue(CorpusManifest manifest, string[] args)
    {
        string field = RequireOption(args, "--field");
        if (field == "combinedExpectedObservationCount")
        {
            Console.WriteLine(manifest.CombinedExpectedObservationCount);
            return 0;
        }

        if (field == "combinedSnapshotFilename")
        {
            Console.WriteLine(manifest.CombinedSnapshotFilename);
            return 0;
        }

        if (field == "candidateCombinedSnapshotFilename")
        {
            Console.WriteLine(manifest.CandidateCombinedSnapshotFilename);
            return 0;
        }

        string probeId = RequireOption(args, "--probe");
        ProbeManifest probe = manifest.Probes.SingleOrDefault(candidate => candidate.Id == probeId)
            ?? throw new ArgumentException($"Unknown probe '{probeId}'.");
        Console.WriteLine(field switch
        {
            "sourceProject" => probe.SourceProject,
            "classification" => probe.Classification,
            "expectedObservationCount" => ProbeCount(manifest, probe).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "expectedSnapshotFilename" => probe.ExpectedSnapshotFilename,
            _ => throw new ArgumentException($"Unknown manifest field '{field}'."),
        });
        return 0;
    }

    private static int Summary(CorpusManifest manifest, bool json)
    {
        IReadOnlyList<AggregateCount> categories = AggregateCategories(manifest);
        var classifications = manifest.Probes
            .GroupBy(probe => probe.Classification, StringComparer.Ordinal)
            .Select(group => new AggregateCount(
                group.Key,
                group.Sum(probe => ProbeCount(manifest, probe))))
            .ToArray();
        var probes = manifest.Probes.Select(probe => new
        {
            probe.Id,
            probe.Classification,
            expectedObservationCount = ProbeCount(manifest, probe),
            probe.SourceProject,
            probe.ExpectedSnapshotFilename,
        });

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                total = manifest.CombinedExpectedObservationCount,
                categories,
                classifications,
                probes,
            }, JsonOptions));
            return 0;
        }

        foreach (AggregateCount category in categories)
        {
            Console.WriteLine($"{category.Name}={category.Count}");
        }

        foreach (AggregateCount classification in classifications)
        {
            Console.WriteLine($"classification.{classification.Name}={classification.Count}");
        }

        Console.WriteLine($"total={manifest.CombinedExpectedObservationCount}");
        return 0;
    }

    private static int ValidateCommand(CorpusManifest manifest, string[] args)
    {
        string probeId = RequireOption(args, "--probe");
        string input = RequireOption(args, "--input");
        string? output = ReadOption(args, "--output");
        ProbeManifest probe = manifest.Probes.SingleOrDefault(candidate => candidate.Id == probeId)
            ?? throw new ArgumentException($"Unknown probe '{probeId}'.");
        IReadOnlyList<string> lines = ValidateSnapshot(manifest, probe, input);
        if (output is not null)
        {
            WriteNormalized(output, lines);
        }

        Console.WriteLine($"behavior-corpus: {probe.Id} snapshot valid; observations={lines.Count}.");
        return 0;
    }

    private static int Combine(CorpusManifest manifest, string[] args)
    {
        string output = RequireOption(args, "--output");
        Dictionary<string, string> inputs = ReadOptions(args, "--input")
            .Select(value => value.Split('=', 2))
            .ToDictionary(
                pieces => pieces.Length == 2 ? pieces[0] : throw new ArgumentException("--input must be probe=path."),
                pieces => pieces[1],
                StringComparer.Ordinal);
        var combined = new List<string>(manifest.CombinedExpectedObservationCount);
        foreach (ProbeManifest probe in manifest.Probes)
        {
            if (!inputs.TryGetValue(probe.Id, out string? path))
            {
                throw new ArgumentException($"Missing --input {probe.Id}=path.");
            }

            combined.AddRange(ValidateSnapshot(manifest, probe, path));
        }

        if (combined.Count != manifest.CombinedExpectedObservationCount)
        {
            throw new InvalidDataException(
                $"Combined snapshot has {combined.Count} observations; expected " +
                $"{manifest.CombinedExpectedObservationCount}.");
        }

        WriteNormalized(output, combined);
        Console.WriteLine($"behavior-corpus: combined {combined.Count} observations into '{Path.GetFullPath(output)}'.");
        return 0;
    }

    private static int Compare(string[] args)
    {
        string referencePath = RequireOption(args, "--reference");
        string candidatePath = RequireOption(args, "--candidate");
        string outputPath = RequireOption(args, "--output");
        IReadOnlyList<string> referenceLines = ReadNormalizedLines(referencePath);
        IReadOnlyList<string> candidateLines = ReadNormalizedLines(candidatePath);
        Dictionary<string, string> reference = ParseObservations(referenceLines, referencePath);
        Dictionary<string, string> candidate = ParseObservations(candidateLines, candidatePath);

        var differences = new List<object>();
        foreach ((string name, string referenceValue) in reference)
        {
            if (!candidate.TryGetValue(name, out string? candidateValue))
            {
                differences.Add(new { kind = "missing", observation = name, reference = referenceValue, candidate = (string?)null });
            }
            else if (!string.Equals(referenceValue, candidateValue, StringComparison.Ordinal))
            {
                differences.Add(new { kind = "different", observation = name, reference = referenceValue, candidate = candidateValue });
            }
        }

        foreach ((string name, string candidateValue) in candidate)
        {
            if (!reference.ContainsKey(name))
            {
                differences.Add(new { kind = "unexpected", observation = name, reference = (string?)null, candidate = candidateValue });
            }
        }

        WriteJson(outputPath, new
        {
            schemaVersion = 1,
            referenceFile = Path.GetFullPath(referencePath),
            candidateFile = Path.GetFullPath(candidatePath),
            referenceObservationCount = reference.Count,
            candidateObservationCount = candidate.Count,
            differenceCount = differences.Count,
            differences,
        });
        Console.WriteLine(
            $"behavior-corpus: compared {reference.Count} reference and {candidate.Count} candidate observations; " +
            $"differences={differences.Count}; report='{Path.GetFullPath(outputPath)}'.");
        return differences.Count == 0 ? 0 : 1;
    }

    private static int GenerateDocumentation(CorpusManifest manifest, string[] args)
    {
        string output = RequireOption(args, "--output");
        string content = BuildDocumentation(manifest);
        if (HasFlag(args, "--check"))
        {
            if (!File.Exists(output) || !string.Equals(
                    NormalizeNewlines(File.ReadAllText(output, Utf8WithoutBom)), content, StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"behavior-corpus: generated documentation is stale: '{Path.GetFullPath(output)}'.");
                return 1;
            }

            Console.WriteLine("behavior-corpus: generated documentation is current.");
            return 0;
        }

        WriteText(output, content);
        Console.WriteLine($"behavior-corpus: wrote '{Path.GetFullPath(output)}'.");
        return 0;
    }

    private static CorpusManifest LoadManifest(string path, string repositoryRoot)
    {
        CorpusManifest manifest = JsonSerializer.Deserialize<CorpusManifest>(
            File.ReadAllText(path, Utf8WithoutBom), JsonOptions)
            ?? throw new InvalidDataException($"Manifest '{path}' is empty.");
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported behavior corpus schema {manifest.SchemaVersion}.");
        }

        RequireUnique(manifest.Probes.Select(probe => probe.Id), "probe");
        RequireUnique(manifest.Categories.Select(category => category.Id), "category");
        RequireUnique(manifest.Probes.Select(probe => probe.ExpectedSnapshotFilename), "snapshot filename");
        Dictionary<string, CategoryManifest> categories = manifest.Categories.ToDictionary(category => category.Id);
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        foreach (ProbeManifest probe in manifest.Probes)
        {
            if (string.IsNullOrWhiteSpace(probe.SourceProject) ||
                !File.Exists(Path.Combine(repositoryRoot, probe.SourceProject.Replace('/', Path.DirectorySeparatorChar))))
            {
                throw new InvalidDataException($"Probe '{probe.Id}' source project does not exist: '{probe.SourceProject}'.");
            }

            foreach (string categoryId in probe.Categories)
            {
                if (!categories.ContainsKey(categoryId))
                {
                    throw new InvalidDataException($"Probe '{probe.Id}' references unknown category '{categoryId}'.");
                }

                if (!assigned.Add(categoryId))
                {
                    throw new InvalidDataException($"Category '{categoryId}' is assigned to more than one probe.");
                }
            }
        }

        if (assigned.Count != manifest.Categories.Count)
        {
            string missing = string.Join(", ", manifest.Categories.Select(category => category.Id).Where(id => !assigned.Contains(id)));
            throw new InvalidDataException($"Unassigned behavior categories: {missing}.");
        }

        foreach (ProbeManifest probe in manifest.Probes)
        {
            string[] prefixes = probe.Categories.SelectMany(id => categories[id].ObservationPrefixes).ToArray();
            RequireUnique(prefixes, $"observation prefix in probe '{probe.Id}'");
        }

        int total = manifest.Categories.Sum(category => category.ExpectedObservationCount);
        if (total != manifest.CombinedExpectedObservationCount)
        {
            throw new InvalidDataException(
                $"Category total {total} does not equal combinedExpectedObservationCount " +
                $"{manifest.CombinedExpectedObservationCount}.");
        }

        return manifest;
    }

    private static IReadOnlyList<string> ValidateSnapshot(
        CorpusManifest manifest,
        ProbeManifest probe,
        string path)
    {
        IReadOnlyList<string> lines = ReadNormalizedLines(path);
        int expectedProbeCount = ProbeCount(manifest, probe);
        if (lines.Count != expectedProbeCount)
        {
            throw new InvalidDataException(
                $"Probe '{probe.Id}' emitted {lines.Count} observations; expected {expectedProbeCount}.");
        }

        Dictionary<string, CategoryManifest> categories = manifest.Categories.ToDictionary(category => category.Id);
        var counts = probe.Categories.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            if (!ObservationLine().IsMatch(line))
            {
                throw new InvalidDataException($"'{path}' contains a non-observation line: '{line}'.");
            }

            string name = line[..line.IndexOf('=')];
            if (!names.Add(name))
            {
                throw new InvalidDataException($"'{path}' contains duplicate observation '{name}'.");
            }

            string[] matches = probe.Categories
                .Where(id => categories[id].ObservationPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidDataException(
                    $"Observation '{name}' matched {matches.Length} categories in probe '{probe.Id}'.");
            }

            counts[matches[0]]++;
        }

        foreach (string categoryId in probe.Categories)
        {
            int expected = categories[categoryId].ExpectedObservationCount;
            if (counts[categoryId] != expected)
            {
                throw new InvalidDataException(
                    $"Category '{categoryId}' emitted {counts[categoryId]} observations in probe '{probe.Id}'; " +
                    $"expected {expected}.");
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> ReadNormalizedLines(string path)
    {
        string text = Utf8WithoutBom.GetString(File.ReadAllBytes(path));
        return NormalizeNewlines(text).Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static Dictionary<string, string> ParseObservations(IReadOnlyList<string> lines, string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            if (!ObservationLine().IsMatch(line))
            {
                throw new InvalidDataException($"'{path}' contains a non-observation line: '{line}'.");
            }

            int separator = line.IndexOf('=');
            if (!result.TryAdd(line[..separator], line[(separator + 1)..]))
            {
                throw new InvalidDataException($"'{path}' contains duplicate observation '{line[..separator]}'.");
            }
        }

        return result;
    }

    private static int ProbeCount(CorpusManifest manifest, ProbeManifest probe)
    {
        Dictionary<string, CategoryManifest> categories = manifest.Categories.ToDictionary(category => category.Id);
        return probe.Categories.Sum(id => categories[id].ExpectedObservationCount);
    }

    private static IReadOnlyList<AggregateCount> AggregateCategories(CorpusManifest manifest)
    {
        var order = new List<string>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (CategoryManifest category in manifest.Categories)
        {
            string id = category.AggregateAs ?? category.Id;
            if (!counts.ContainsKey(id))
            {
                order.Add(id);
                names.Add(id, category.DisplayName);
                counts.Add(id, 0);
            }

            counts[id] += category.ExpectedObservationCount;
        }

        return order.Select(id => new AggregateCount(names[id], counts[id])).ToArray();
    }

    private static string BuildDocumentation(CorpusManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!-- Generated by tools/behavior-corpus from tests/behavior-corpus-counts.json. Do not edit. -->");
        builder.AppendLine("# Behavior corpus manifest");
        builder.AppendLine();
        builder.AppendLine("## Categories");
        builder.AppendLine();
        builder.AppendLine("| Category | Observations |");
        builder.AppendLine("| --- | ---: |");
        foreach (AggregateCount category in AggregateCategories(manifest))
        {
            builder.AppendLine($"| {EscapeTable(category.Name)} | {category.Count} |");
        }

        builder.AppendLine($"| **Total** | **{manifest.CombinedExpectedObservationCount}** |");
        builder.AppendLine();
        builder.AppendLine("## Probes");
        builder.AppendLine();
        builder.AppendLine("| Classification | Source project | Observations | XNA snapshot | Asset requirement |");
        builder.AppendLine("| --- | --- | ---: | --- | --- |");
        foreach (ProbeManifest probe in manifest.Probes)
        {
            builder.AppendLine(
                $"| {EscapeTable(probe.Classification)} | `{EscapeTable(probe.SourceProject)}` | " +
                $"{ProbeCount(manifest, probe)} | `{EscapeTable(probe.ExpectedSnapshotFilename)}` | " +
                $"{EscapeTable(probe.AssetRequirement)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Comparator status");
        builder.AppendLine();
        builder.AppendLine("| Probe | XNA runtime requirement | FNA | MonoGame |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (ProbeManifest probe in manifest.Probes)
        {
            builder.AppendLine(
                $"| `{probe.Id}` | {EscapeTable(probe.XnaRuntimeRequirement)} | " +
                $"{EscapeTable(probe.FnaSupportStatus)} | {EscapeTable(probe.MonoGameSupportStatus)} |");
        }

        return builder.ToString();
    }

    private static void WriteNormalized(string path, IEnumerable<string> lines) =>
        WriteText(path, string.Join('\n', lines) + "\n");

    private static void WriteJson(string path, object value) =>
        WriteText(path, JsonSerializer.Serialize(value, JsonOptions) + "\n");

    private static void WriteText(string path, string content)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, NormalizeNewlines(content), Utf8WithoutBom);
    }

    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n").Replace('\r', '\n');

    private static string EscapeTable(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static void RequireUnique(IEnumerable<string> values, string description)
    {
        string[] duplicates = values.GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidDataException($"Duplicate {description} values: {string.Join(", ", duplicates)}.");
        }
    }

    private static string RequireOption(string[] args, string name) =>
        ReadOption(args, name) ?? throw new ArgumentException($"{name} is required.");

    private static string? ReadOption(string[] args, string name)
    {
        for (int index = 1; index < args.Length; index++)
        {
            if (args[index] == name)
            {
                if (++index >= args.Length)
                {
                    throw new ArgumentException($"{name} requires a value.");
                }

                return args[index];
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadOptions(string[] args, string name)
    {
        var values = new List<string>();
        for (int index = 1; index < args.Length; index++)
        {
            if (args[index] == name)
            {
                if (++index >= args.Length)
                {
                    throw new ArgumentException($"{name} requires a value.");
                }

                values.Add(args[index]);
            }
        }

        return values;
    }

    private static bool HasFlag(string[] args, string name) => args.Contains(name, StringComparer.Ordinal);

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

        throw new InvalidOperationException("Could not find CNA.sln above the current directory.");
    }

    private static void PrintHelp() => Console.WriteLine(
        """
        CNA behavior corpus manifest utility

        Commands:
          verify [--manifest path]
          get --field name [--probe id]
          summary [--json] [--manifest path]
          validate --probe id --input file [--output normalized-file]
          combine --input probe=file [--input probe=file ...] --output file
          compare --reference file --candidate file --output report.json
          docs --output file [--check]

        validate/combine enforce exact probe and per-category counts. All written text is UTF-8
        without BOM with LF line endings. compare exits 1 when any observation is missing,
        unexpected, or has a different value.
        """);

    [GeneratedRegex("^[a-z][a-z0-9_.]*=.*$", RegexOptions.CultureInvariant)]
    private static partial Regex ObservationLine();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

internal sealed class CorpusManifest
{
    public int SchemaVersion { get; init; }
    public string Description { get; init; } = string.Empty;
    public int CombinedExpectedObservationCount { get; init; }
    public string CombinedSnapshotFilename { get; init; } = string.Empty;
    public string CandidateCombinedSnapshotFilename { get; init; } = string.Empty;
    public List<ProbeManifest> Probes { get; init; } = [];
    public List<CategoryManifest> Categories { get; init; } = [];
}

internal sealed class ProbeManifest
{
    public string Id { get; init; } = string.Empty;
    public string SourceProject { get; init; } = string.Empty;
    public string Classification { get; init; } = string.Empty;
    public string XnaRuntimeRequirement { get; init; } = string.Empty;
    public string FnaSupportStatus { get; init; } = string.Empty;
    public string MonoGameSupportStatus { get; init; } = string.Empty;
    public string AssetRequirement { get; init; } = string.Empty;
    public string ExpectedSnapshotFilename { get; init; } = string.Empty;
    public List<string> Categories { get; init; } = [];
}

internal sealed class CategoryManifest
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int ExpectedObservationCount { get; init; }
    public string? AggregateAs { get; init; }
    public List<string> ObservationPrefixes { get; init; } = [];
}

internal sealed record AggregateCount(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("count")] int Count);
