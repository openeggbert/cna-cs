using System.Text.Json;

namespace CNA.ApiCompat;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            CommandLine options = CommandLine.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            ResolvedInputs inputs = options.Resolve();
            var reader = new MetadataContractReader(inputs.NamespacePrefixes);
            ApiContract target = reader.Read([inputs.TargetPath]);
            ApiContract? reference = null;
            List<Diagnostic> diagnostics;

            if (options.LeakOnly)
            {
                diagnostics = ContractComparer.FindCnaLeaks(target);
            }
            else
            {
                reference = reader.Read(inputs.ReferencePaths);
                diagnostics = ContractComparer.Compare(reference, target);
            }

            AllowlistDocument allowlist = inputs.AllowlistPath is null
                ? new AllowlistDocument()
                : JsonSerializer.Deserialize<AllowlistDocument>(
                    File.ReadAllText(inputs.AllowlistPath),
                    CommandLine.JsonOptions()) ?? new AllowlistDocument();
            ContractComparer.ApplyAllowlist(diagnostics, allowlist);

            PrintResult(options.Format, inputs, reference, target, diagnostics, options.LeakOnly);
            int unallowed = diagnostics.Count(diagnostic => !diagnostic.IsAllowed);
            return options.ReportOnly || unallowed == 0 ? 0 : 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or JsonException)
        {
            Console.Error.WriteLine($"api-compat configuration error: {exception.Message}");
            return 2;
        }
    }

    private static void PrintResult(
        string format,
        ResolvedInputs inputs,
        ApiContract? reference,
        ApiContract target,
        IReadOnlyList<Diagnostic> diagnostics,
        bool leakOnly)
    {
        int allowed = diagnostics.Count(diagnostic => diagnostic.IsAllowed);
        int failures = diagnostics.Count - allowed;
        var counts = diagnostics
            .GroupBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        if (format == "json")
        {
            var report = new
            {
                profile = leakOnly ? "CNA public API leak check" : inputs.Profile.Name,
                target = inputs.TargetPath,
                references = inputs.ReferencePaths,
                namespacePrefixes = inputs.NamespacePrefixes,
                referenceTypeCount = reference?.Types.Count,
                targetTypeCount = target.Types.Count,
                diagnosticCount = diagnostics.Count,
                allowedCount = allowed,
                failureCount = failures,
                counts,
                diagnostics = diagnostics.Select(diagnostic => new
                {
                    diagnostic.Code,
                    diagnostic.Subject,
                    diagnostic.Expected,
                    diagnostic.Actual,
                    diagnostic.Message,
                    allowed = diagnostic.IsAllowed,
                }),
            };
            Console.WriteLine(JsonSerializer.Serialize(report, CommandLine.JsonOptions()));
            return;
        }

        if (format == "github")
        {
            foreach (Diagnostic diagnostic in diagnostics.Where(diagnostic => !diagnostic.IsAllowed))
            {
                Console.WriteLine($"::error title={Escape(diagnostic.Code)}::{Escape(diagnostic.Subject)}: {Escape(diagnostic.Message)} " +
                                  $"Expected={Escape(diagnostic.Expected)} Actual={Escape(diagnostic.Actual)}");
            }
        }
        else
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                string status = diagnostic.IsAllowed ? "ALLOW" : "DIFF ";
                Console.WriteLine($"{status} {diagnostic.Code} | {diagnostic.Subject}");
                if (diagnostic.Expected is not null) Console.WriteLine($"      expected: {diagnostic.Expected}");
                if (diagnostic.Actual is not null) Console.WriteLine($"      actual:   {diagnostic.Actual}");
            }
        }

        Console.WriteLine(
            $"api-compat: profile='{(leakOnly ? "leak-only" : inputs.Profile.Name)}', " +
            $"reference types={reference?.Types.Count.ToString() ?? "n/a"}, target types={target.Types.Count}, " +
            $"diagnostics={diagnostics.Count}, allowed={allowed}, failures={failures}.");
        if (counts.Count > 0)
        {
            Console.WriteLine("api-compat counts: " + string.Join(", ", counts.Select(pair => $"{pair.Key}={pair.Value}")));
        }
    }

    private static string Escape(string? value) =>
        (value ?? "<null>").Replace("%", "%25", StringComparison.Ordinal)
            .Replace("\r", "%0D", StringComparison.Ordinal)
            .Replace("\n", "%0A", StringComparison.Ordinal);

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            CNA XNA metadata contract verifier

            Usage:
              dotnet run --project tools/api-compat -- [options]

            Options:
              --target <dll>          CNA.XnaCompat assembly. Defaults to the Release build or
                                      CNA_XNACOMPAT_ASSEMBLY.
              --reference <dll>       Reference assembly; repeat for XNA's split assemblies.
              --reference-dir <dir>   Directory containing the profile's assemblies. Defaults to
                                      XNA_REFERENCE_PATH.
              --profile <json>        Compatibility profile manifest.
              --allowlist <json>      Reviewed exception allowlist.
              --namespace <prefix>    Included public namespace prefix; repeat as needed.
              --format <kind>         text (default), json, or github.
              --leak-only             Check public/protected signatures for CNA.* types without a
                                      reference assembly.
              --report-only           Print differences but return success (audit use only).
              --help                  Show this help.

            Exit codes: 0 clean/allowed, 1 unallowed compatibility differences, 2 configuration error.
            """);
    }
}
