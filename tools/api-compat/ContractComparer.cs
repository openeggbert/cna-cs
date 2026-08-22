namespace CNA.ApiCompat;

internal static class ContractComparer
{
    public static List<Diagnostic> Compare(ApiContract reference, ApiContract target)
    {
        var diagnostics = new List<Diagnostic>();

        foreach ((string name, TypeContract referenceType) in reference.Types)
        {
            if (!target.Types.TryGetValue(name, out TypeContract? targetType))
            {
                diagnostics.Add(new Diagnostic(
                    "MISSING_TYPE",
                    name,
                    referenceType.AssemblyName,
                    null,
                    $"Reference type '{name}' is missing."));
                continue;
            }

            CompareType(referenceType, targetType, diagnostics);
        }

        foreach ((string name, TypeContract targetType) in target.Types)
        {
            if (!reference.Types.ContainsKey(name))
            {
                diagnostics.Add(new Diagnostic(
                    "UNEXPECTED_TYPE",
                    name,
                    null,
                    targetType.AssemblyName,
                    $"Target exposes type '{name}', which is absent from the selected XNA profile."));
            }
        }

        diagnostics.AddRange(FindCnaLeaks(target));
        return diagnostics
            .OrderBy(diagnostic => diagnostic.Subject, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Expected, StringComparer.Ordinal)
            .ToList();
    }

    public static List<Diagnostic> FindCnaLeaks(ApiContract target)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (TypeContract type in target.Types.Values)
        {
            AddLeakIfPresent(diagnostics, type.Name, "base type", type.BaseType);
            foreach (string @interface in type.Interfaces)
            {
                AddLeakIfPresent(diagnostics, type.Name, "implemented interface", @interface);
            }

            foreach (string constraint in type.GenericParameters)
            {
                AddLeakIfPresent(diagnostics, type.Name, "generic constraint", constraint);
            }

            foreach (MemberContract member in type.Members)
            {
                string subject = $"{type.Name}::{member.DisplayName}";
                AddLeakIfPresent(diagnostics, subject, $"{member.Kind} type", member.ValueType);
                foreach (ParameterContract parameter in member.Parameters)
                {
                    AddLeakIfPresent(diagnostics, subject, $"parameter '{parameter.Name}'", parameter.Type);
                }

                foreach (string constraint in member.GenericParameters)
                {
                    AddLeakIfPresent(diagnostics, subject, "generic constraint", constraint);
                }
            }
        }

        return diagnostics;
    }

    public static void ApplyAllowlist(List<Diagnostic> diagnostics, AllowlistDocument allowlist)
    {
        var duplicateKeys = allowlist.Exceptions
            .GroupBy(entry => $"{entry.Code}\0{entry.Subject}\0{entry.Expected}\0{entry.Actual}", StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.Replace('\0', '|'))
            .ToArray();
        if (duplicateKeys.Length > 0)
        {
            throw new InvalidDataException("Duplicate allowlist entries: " + string.Join(", ", duplicateKeys));
        }

        foreach (AllowlistEntry entry in allowlist.Exceptions)
        {
            if (string.IsNullOrWhiteSpace(entry.Code) || string.IsNullOrWhiteSpace(entry.Subject) ||
                string.IsNullOrWhiteSpace(entry.Rationale))
            {
                throw new InvalidDataException(
                    "Every allowlist entry requires non-empty code, subject, and rationale values.");
            }
        }

        foreach (Diagnostic diagnostic in diagnostics)
        {
            AllowlistEntry? match = allowlist.Exceptions.FirstOrDefault(entry => entry.Matches(diagnostic));
            if (match is not null)
            {
                diagnostic.IsAllowed = true;
                match.WasUsed = true;
            }
        }

        foreach (AllowlistEntry stale in allowlist.Exceptions.Where(entry => !entry.WasUsed))
        {
            diagnostics.Add(new Diagnostic(
                "STALE_ALLOWLIST",
                $"{stale.Code}|{stale.Subject}",
                "a matching current diagnostic",
                "no match",
                $"Allowlist entry no longer matches a diagnostic: {stale.Rationale}"));
        }
    }

    private static void CompareType(
        TypeContract expected,
        TypeContract actual,
        List<Diagnostic> diagnostics)
    {
        CompareValue(diagnostics, "TYPE_KIND_MISMATCH", expected.Name, expected.Kind, actual.Kind, "type kind");
        CompareValue(
            diagnostics,
            "TYPE_ACCESSIBILITY_MISMATCH",
            expected.Name,
            expected.Accessibility,
            actual.Accessibility,
            "type accessibility");
        CompareValue(diagnostics, "BASE_TYPE_MISMATCH", expected.Name, expected.BaseType, actual.BaseType, "base type");
        CompareSequence(
            diagnostics,
            "INTERFACE_MISMATCH",
            expected.Name,
            expected.Interfaces,
            actual.Interfaces,
            "implemented interfaces");
        CompareValue(
            diagnostics,
            "TYPE_MODIFIER_MISMATCH",
            expected.Name,
            FormatTypeModifiers(expected),
            FormatTypeModifiers(actual),
            "abstract/sealed modifiers");
        CompareSequence(
            diagnostics,
            "TYPE_GENERIC_MISMATCH",
            expected.Name,
            expected.GenericParameters,
            actual.GenericParameters,
            "generic parameters and constraints");
        CompareValue(diagnostics, "TYPE_LAYOUT_MISMATCH", expected.Name, expected.Layout, actual.Layout, "type layout");
        CompareSequence(
            diagnostics,
            "TYPE_ATTRIBUTE_MISMATCH",
            expected.Name,
            expected.Attributes,
            actual.Attributes,
            "relevant attributes");

        CompareMembers(expected, actual, diagnostics);
    }

    private static void CompareMembers(
        TypeContract expectedType,
        TypeContract actualType,
        List<Diagnostic> diagnostics)
    {
        Dictionary<string, List<MemberContract>> expectedGroups = expectedType.Members
            .GroupBy(member => member.FamilyKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        Dictionary<string, List<MemberContract>> actualGroups = actualType.Members
            .GroupBy(member => member.FamilyKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (string family in expectedGroups.Keys.Union(actualGroups.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var expected = expectedGroups.GetValueOrDefault(family, []);
            var actual = actualGroups.GetValueOrDefault(family, []);
            var pairs = new List<(MemberContract Expected, MemberContract Actual, bool Exact)>();

            for (int index = expected.Count - 1; index >= 0; index--)
            {
                int match = actual.FindIndex(candidate => candidate.SignatureKey == expected[index].SignatureKey);
                if (match < 0)
                {
                    continue;
                }

                pairs.Add((expected[index], actual[match], true));
                expected.RemoveAt(index);
                actual.RemoveAt(match);
            }

            while (expected.Count > 0 && actual.Count > 0)
            {
                (int expectedIndex, int actualIndex) = FindClosestPair(expected, actual);
                pairs.Add((expected[expectedIndex], actual[actualIndex], false));
                expected.RemoveAt(expectedIndex);
                actual.RemoveAt(actualIndex);
            }

            foreach ((MemberContract expectedMember, MemberContract actualMember, bool exact) in pairs)
            {
                string subject = $"{expectedType.Name}::{expectedMember.DisplayName}";
                if (!exact)
                {
                    diagnostics.Add(new Diagnostic(
                        "PARAMETER_MISMATCH",
                        subject,
                        FormatParameters(expectedMember.Parameters),
                        FormatParameters(actualMember.Parameters),
                        $"Parameter contract differs for {expectedMember.Kind} '{subject}'."));
                }

                CompareMember(subject, expectedMember, actualMember, diagnostics);
            }

            foreach (MemberContract missing in expected)
            {
                string subject = $"{expectedType.Name}::{missing.DisplayName}";
                diagnostics.Add(new Diagnostic(
                    "MISSING_MEMBER",
                    subject,
                    FormatMember(missing),
                    null,
                    $"Reference {missing.Kind} '{subject}' is missing."));
            }

            foreach (MemberContract extra in actual)
            {
                string subject = $"{actualType.Name}::{extra.DisplayName}";
                diagnostics.Add(new Diagnostic(
                    "UNEXPECTED_MEMBER",
                    subject,
                    null,
                    FormatMember(extra),
                    $"Target exposes extra {extra.Kind} '{subject}'."));
            }
        }
    }

    private static void CompareMember(
        string subject,
        MemberContract expected,
        MemberContract actual,
        List<Diagnostic> diagnostics)
    {
        string valueCode = expected.Kind switch
        {
            "property" => "PROPERTY_TYPE_MISMATCH",
            "event" => "EVENT_TYPE_MISMATCH",
            "field" => "FIELD_TYPE_MISMATCH",
            _ => "RETURN_TYPE_MISMATCH",
        };
        CompareValue(diagnostics, valueCode, subject, expected.ValueType, actual.ValueType, "declared value/return type");
        CompareValue(
            diagnostics,
            "MEMBER_ACCESSIBILITY_MISMATCH",
            subject,
            expected.Accessibility,
            actual.Accessibility,
            "member accessibility");
        CompareValue(
            diagnostics,
            "MEMBER_MODIFIER_MISMATCH",
            subject,
            FormatMemberModifiers(expected),
            FormatMemberModifiers(actual),
            "static/abstract/virtual/final modifiers");
        CompareSequence(
            diagnostics,
            "METHOD_GENERIC_MISMATCH",
            subject,
            expected.GenericParameters,
            actual.GenericParameters,
            "generic parameters and constraints");

        if (expected.Parameters.Count == actual.Parameters.Count)
        {
            for (int index = 0; index < expected.Parameters.Count; index++)
            {
                ParameterContract expectedParameter = expected.Parameters[index];
                ParameterContract actualParameter = actual.Parameters[index];
                CompareValue(
                    diagnostics,
                    "PARAMETER_NAME_MISMATCH",
                    $"{subject} parameter {index}",
                    expectedParameter.Name,
                    actualParameter.Name,
                    "parameter name");
                CompareValue(
                    diagnostics,
                    "PARAMETER_DEFAULT_MISMATCH",
                    $"{subject} parameter {index}",
                    FormatDefault(expectedParameter),
                    FormatDefault(actualParameter),
                    "optional/default contract");
            }
        }

        if (expected.Kind == "property")
        {
            CompareValue(
                diagnostics,
                "PROPERTY_ACCESSOR_MISMATCH",
                subject,
                $"get={expected.GetterAccessibility};set={expected.SetterAccessibility}",
                $"get={actual.GetterAccessibility};set={actual.SetterAccessibility}",
                "property accessor accessibility");
        }

        if (expected.Kind == "event")
        {
            CompareValue(
                diagnostics,
                "EVENT_ACCESSOR_MISMATCH",
                subject,
                $"add={expected.AdderAccessibility};remove={expected.RemoverAccessibility}",
                $"add={actual.AdderAccessibility};remove={actual.RemoverAccessibility}",
                "event accessor accessibility");
        }

        if (expected.Kind == "field")
        {
            CompareValue(
                diagnostics,
                "FIELD_MODIFIER_MISMATCH",
                subject,
                $"readonly={expected.IsReadOnly};literal={expected.IsLiteral}",
                $"readonly={actual.IsReadOnly};literal={actual.IsLiteral}",
                "field modifiers");
            CompareValue(
                diagnostics,
                "FIELD_CONSTANT_MISMATCH",
                subject,
                expected.ConstantValue,
                actual.ConstantValue,
                "field/enum constant value");
        }

        CompareSequence(
            diagnostics,
            "MEMBER_ATTRIBUTE_MISMATCH",
            subject,
            expected.Attributes,
            actual.Attributes,
            "relevant attributes");
    }

    private static (int ExpectedIndex, int ActualIndex) FindClosestPair(
        IReadOnlyList<MemberContract> expected,
        IReadOnlyList<MemberContract> actual)
    {
        int bestExpected = 0;
        int bestActual = 0;
        int bestDistance = int.MaxValue;
        for (int expectedIndex = 0; expectedIndex < expected.Count; expectedIndex++)
        {
            for (int actualIndex = 0; actualIndex < actual.Count; actualIndex++)
            {
                int distance = ParameterDistance(expected[expectedIndex], actual[actualIndex]);
                if (distance < bestDistance)
                {
                    bestExpected = expectedIndex;
                    bestActual = actualIndex;
                    bestDistance = distance;
                }
            }
        }

        return (bestExpected, bestActual);
    }

    private static int ParameterDistance(MemberContract expected, MemberContract actual)
    {
        int distance = 0;
        for (int index = 0; index < expected.Parameters.Count; index++)
        {
            if (expected.Parameters[index].Type != actual.Parameters[index].Type) distance += 4;
            if (expected.Parameters[index].Modifier != actual.Parameters[index].Modifier) distance += 2;
            if (expected.Parameters[index].Name != actual.Parameters[index].Name) distance += 1;
        }

        return distance;
    }

    private static void AddLeakIfPresent(
        List<Diagnostic> diagnostics,
        string subject,
        string location,
        string? value)
    {
        if (value is null || !ContainsCnaType(value))
        {
            return;
        }

        diagnostics.Add(new Diagnostic(
            "CNA_TYPE_LEAK",
            subject,
            "no CNA.* types",
            value,
            $"Public/protected {location} exposes CNA implementation type '{value}'."));
    }

    private static bool ContainsCnaType(string value)
    {
        int index = value.IndexOf("CNA.", StringComparison.Ordinal);
        while (index >= 0)
        {
            if (index == 0 || !char.IsLetterOrDigit(value[index - 1]) && value[index - 1] != '_')
            {
                return true;
            }

            index = value.IndexOf("CNA.", index + 4, StringComparison.Ordinal);
        }

        return false;
    }

    private static void CompareSequence(
        List<Diagnostic> diagnostics,
        string code,
        string subject,
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual,
        string description) =>
        CompareValue(
            diagnostics,
            code,
            subject,
            string.Join(", ", expected),
            string.Join(", ", actual),
            description);

    private static void CompareValue(
        List<Diagnostic> diagnostics,
        string code,
        string subject,
        string? expected,
        string? actual,
        string description)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return;
        }

        diagnostics.Add(new Diagnostic(
            code,
            subject,
            expected,
            actual,
            $"{description} differs for '{subject}'."));
    }

    private static string FormatTypeModifiers(TypeContract type) =>
        $"abstract={type.IsAbstract};sealed={type.IsSealed}";

    private static string FormatMemberModifiers(MemberContract member) =>
        $"static={member.IsStatic};abstract={member.IsAbstract};virtual={member.IsVirtual};final={member.IsFinal}";

    private static string FormatDefault(ParameterContract parameter) =>
        $"optional={parameter.IsOptional};hasDefault={parameter.HasDefault};value={parameter.DefaultValue}";

    private static string FormatParameters(IReadOnlyList<ParameterContract> parameters) =>
        string.Join(", ", parameters.Select(parameter => parameter.Display));

    private static string FormatMember(MemberContract member) =>
        $"{member.Accessibility} {FormatMemberModifiers(member)} {member.ValueType} {member.DisplayName}";
}
