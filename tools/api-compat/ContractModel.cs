using System.Text.Json.Serialization;

namespace CNA.ApiCompat;

internal sealed class ApiContract
{
    public SortedDictionary<string, TypeContract> Types { get; } = new(StringComparer.Ordinal);
}

internal sealed record TypeContract(
    string Name,
    string Accessibility,
    string Kind,
    bool IsAbstract,
    bool IsSealed,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> GenericParameters,
    string Layout,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<MemberContract> Members,
    string AssemblyName);

internal sealed record MemberContract(
    string Kind,
    string Name,
    int GenericArity,
    string Accessibility,
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsFinal,
    string? ValueType,
    IReadOnlyList<ParameterContract> Parameters,
    IReadOnlyList<string> GenericParameters,
    string? GetterAccessibility,
    string? SetterAccessibility,
    string? AdderAccessibility,
    string? RemoverAccessibility,
    bool IsReadOnly,
    bool IsLiteral,
    string? ConstantValue,
    IReadOnlyList<string> Attributes)
{
    public string FamilyKey => $"{Kind}|{Name}|{GenericArity}|{Parameters.Count}";

    public string SignatureKey =>
        $"{FamilyKey}|{string.Join(",", Parameters.Select(parameter => $"{parameter.Modifier}:{parameter.Type}"))}";

    public string DisplayName => Parameters.Count == 0 && Kind is "field" or "event"
        ? Name
        : $"{Name}{(GenericArity == 0 ? string.Empty : $"``{GenericArity}")}" +
          $"({string.Join(", ", Parameters.Select(parameter => parameter.Display))})";
}

internal sealed record ParameterContract(
    int Position,
    string Name,
    string Type,
    string Modifier,
    bool IsOptional,
    bool HasDefault,
    string? DefaultValue)
{
    public string Display =>
        $"{(Modifier == "value" ? string.Empty : Modifier + " ")}{Type} {Name}" +
        (IsOptional || HasDefault ? $" = {(HasDefault ? DefaultValue : "<missing>")}" : string.Empty);
}

internal sealed record Diagnostic(
    string Code,
    string Subject,
    string? Expected,
    string? Actual,
    string Message)
{
    [JsonIgnore]
    public bool IsAllowed { get; set; }
}

internal sealed class AllowlistDocument
{
    [JsonPropertyName("exceptions")]
    public List<AllowlistEntry> Exceptions { get; init; } = [];
}

internal sealed class AllowlistEntry
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = string.Empty;

    [JsonPropertyName("expected")]
    public string? Expected { get; init; }

    [JsonPropertyName("actual")]
    public string? Actual { get; init; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; init; } = string.Empty;

    [JsonIgnore]
    public bool WasUsed { get; set; }

    public bool Matches(Diagnostic diagnostic) =>
        string.Equals(Code, diagnostic.Code, StringComparison.Ordinal) &&
        string.Equals(Subject, diagnostic.Subject, StringComparison.Ordinal) &&
        (Expected is null || string.Equals(Expected, diagnostic.Expected, StringComparison.Ordinal)) &&
        (Actual is null || string.Equals(Actual, diagnostic.Actual, StringComparison.Ordinal));
}

internal sealed class CompatibilityProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("referenceAssemblies")]
    public List<string> ReferenceAssemblies { get; init; } = [];

    [JsonPropertyName("namespacePrefixes")]
    public List<string> NamespacePrefixes { get; init; } = [];

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}
