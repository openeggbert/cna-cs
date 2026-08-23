using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CNA.ApiCompat;

string? manifestPath = null;
string? referenceDirectory = null;
string? jsonOutput = null;
string? markdownOutput = null;
for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--manifest": manifestPath = Value(ref index); break;
        case "--reference-dir": referenceDirectory = Value(ref index); break;
        case "--json": jsonOutput = Value(ref index); break;
        case "--markdown": markdownOutput = Value(ref index); break;
        default: throw new ArgumentException($"Unknown argument: {args[index]}");
    }
}

if (manifestPath is null || referenceDirectory is null || jsonOutput is null || markdownOutput is null)
{
    Console.Error.WriteLine("Usage: CNA.ProfileInventory --manifest <profiles.json> --reference-dir <dir> --json <out.json> --markdown <out.md>");
    return 2;
}

manifestPath = Path.GetFullPath(manifestPath);
referenceDirectory = Path.GetFullPath(referenceDirectory);
InventoryManifest manifest = JsonSerializer.Deserialize<InventoryManifest>(File.ReadAllText(manifestPath), JsonOptions())
    ?? throw new InvalidDataException("Profile manifest was empty.");
string baselinePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestPath)!, manifest.BaselineProfile));
CompatibilityProfile baselineProfile = JsonSerializer.Deserialize<CompatibilityProfile>(File.ReadAllText(baselinePath), JsonOptions())
    ?? throw new InvalidDataException("Baseline profile was empty.");
string[] baselineAssemblies = baselineProfile.ReferenceAssemblies.Select(name => Path.Combine(referenceDirectory, name)).ToArray();
foreach (string path in baselineAssemblies)
{
    if (!File.Exists(path)) throw new FileNotFoundException("Baseline reference assembly is missing.", path);
}
ApiContract baseline = new MetadataContractReader(baselineProfile.NamespacePrefixes).Read(baselineAssemblies);

List<ProfileResult> results = [];
foreach (ProfileDefinition profile in manifest.Profiles)
{
    if (!profile.InventoryEnabled)
    {
        results.Add(new ProfileResult(profile, "not-configured", null, null, null, []));
        continue;
    }

    string[] paths = profile.ProfileAssemblies.Select(name => Path.Combine(referenceDirectory, name)).ToArray();
    string[] missing = paths.Where(path => !File.Exists(path)).Select(Path.GetFileName).ToArray()!;
    if (missing.Length > 0)
    {
        results.Add(new ProfileResult(profile, $"not-configured (missing: {string.Join(", ", missing)})", null, null, null, []));
        continue;
    }

    ApiContract contract = new MetadataContractReader(profile.NamespacePrefixes).Read(paths);
    int memberCount = contract.Types.Values.Sum(type => type.Members.Count);
    int overlap = contract.Types.Keys.Intersect(baseline.Types.Keys, StringComparer.Ordinal).Count();
    List<AssemblyEvidence> assemblies = paths.Select(ReadAssemblyEvidence).ToList();
    results.Add(new ProfileResult(profile, "measured", contract.Types.Count, memberCount, overlap, assemblies));
}

var report = new
{
    schemaVersion = 1,
    generatedAtUtc = DateTimeOffset.UtcNow,
    referenceDirectoryEvidence = "caller-supplied legal local reference assemblies; binaries are not copied into this repository",
    baseline = new { profile = baselineProfile.Name, typeCount = baseline.Types.Count, assemblyCount = baselineAssemblies.Length },
    profiles = results.Select(result => new
    {
        result.Definition.Id,
        result.Definition.Name,
        result.Definition.Platform,
        referenceAssemblies = result.Definition.ProfileAssemblies,
        result.Definition.ReferenceAssemblySetStatus,
        result.Definition.ServiceAvailability,
        result.Definition.RecommendedSupportStatus,
        evidenceStatus = result.Status,
        typeCount = result.TypeCount,
        memberCount = result.MemberCount,
        overlapWithCurrentRuntimeTypes = result.Overlap,
        assemblies = result.Assemblies,
    })
};

WriteUtf8(jsonOutput, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
StringBuilder markdown = new();
markdown.AppendLine("# Additional XNA profile inventory");
markdown.AppendLine();
markdown.AppendLine("Generated from `tools/profile-inventory/profiles.json`. The completed seven-assembly Windows runtime baseline remains separate and unchanged at 257 types. Microsoft reference binaries are never copied into this repository.");
markdown.AppendLine();
markdown.AppendLine("| Profile | Reference assemblies | Evidence | Types | Members | Baseline overlap | Recommended status |");
markdown.AppendLine("| --- | ---: | --- | ---: | ---: | ---: | --- |");
foreach (ProfileResult result in results)
{
    markdown.Append("| ").Append(result.Definition.Name).Append(" | ")
        .Append(result.Definition.ProfileAssemblies.Count).Append(" | ").Append(result.Status).Append(" | ")
        .Append(result.TypeCount?.ToString() ?? "pending").Append(" | ")
        .Append(result.MemberCount?.ToString() ?? "pending").Append(" | ")
        .Append(result.Overlap?.ToString() ?? "pending").Append(" | ")
        .Append(result.Definition.RecommendedSupportStatus.Replace("|", "\\|", StringComparison.Ordinal)).AppendLine(" |");
}
markdown.AppendLine();
foreach (ProfileResult result in results)
{
    markdown.Append("## ").AppendLine(result.Definition.Name).AppendLine();
    markdown.Append("- Platform/service availability: ").AppendLine(result.Definition.ServiceAvailability);
    markdown.Append("- Reference set: ").AppendLine(result.Definition.ReferenceAssemblySetStatus);
    markdown.Append("- Assemblies: ").AppendLine(result.Definition.ProfileAssemblies.Count == 0
        ? "pending authoritative reference pack"
        : string.Join(", ", result.Definition.ProfileAssemblies.Select(name => $"`{name}`")));
    foreach (AssemblyEvidence assembly in result.Assemblies)
    {
        markdown.Append("  - `").Append(assembly.FileName).Append("`: ").Append(assembly.Version)
            .Append(", SHA-256 `").Append(assembly.Sha256).AppendLine("`");
    }
    markdown.AppendLine();
}
WriteUtf8(markdownOutput, markdown.ToString().TrimEnd() + "\n");

Console.WriteLine($"PROFILE_BASELINE_TYPES={baseline.Types.Count}");
foreach (ProfileResult result in results)
{
    Console.WriteLine($"PROFILE_{result.Definition.Id.ToUpperInvariant().Replace('-', '_')}={result.Status};types={result.TypeCount?.ToString() ?? "pending"};members={result.MemberCount?.ToString() ?? "pending"};overlap={result.Overlap?.ToString() ?? "pending"}");
}
return 0;

string Value(ref int index)
{
    if (++index >= args.Length) throw new ArgumentException($"Missing value after {args[index - 1]}");
    return args[index];
}

static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

static void WriteUtf8(string path, string content)
{
    path = Path.GetFullPath(path);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, content.Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
}

static AssemblyEvidence ReadAssemblyEvidence(string path)
{
    using FileStream stream = File.OpenRead(path);
    using PEReader pe = new(stream);
    MetadataReader metadata = pe.GetMetadataReader();
    AssemblyDefinition definition = metadata.GetAssemblyDefinition();
    return new AssemblyEvidence(
        Path.GetFileName(path),
        metadata.GetString(definition.Name),
        definition.Version.ToString(),
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
}

internal sealed class InventoryManifest
{
    public int SchemaVersion { get; init; }
    public string Description { get; init; } = string.Empty;
    public string BaselineProfile { get; init; } = string.Empty;
    public List<ProfileDefinition> Profiles { get; init; } = [];
}

internal sealed class ProfileDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public List<string> ProfileAssemblies { get; init; } = [];
    public List<string> NamespacePrefixes { get; init; } = [];
    public string ReferenceAssemblySetStatus { get; init; } = string.Empty;
    public string ServiceAvailability { get; init; } = string.Empty;
    public string RecommendedSupportStatus { get; init; } = string.Empty;
    public bool InventoryEnabled { get; init; }
}

internal sealed record ProfileResult(
    ProfileDefinition Definition,
    string Status,
    int? TypeCount,
    int? MemberCount,
    int? Overlap,
    IReadOnlyList<AssemblyEvidence> Assemblies);

internal sealed record AssemblyEvidence(string FileName, string AssemblyName, string Version, string Sha256);
