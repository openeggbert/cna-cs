using System.Reflection;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// The versioned, consumer-specific contract between this assembly and CNA's native C ABI.
/// Version admission is deliberately a reviewed matrix rather than a SemVer range: CNA documents
/// that an experimental 0.x minor may be incompatible.
/// </summary>
internal static class CnaNativeAbiPolicy
{
    internal const string PolicyVersion = "cna-cs-native-abi/1";
    internal const uint ConsumerVersion = (0u << 16) | (20u << 8) | 0u;

    /// <summary>
    /// The reviewed matrix. It is a point list, never a range: CNA documents that an experimental
    /// 0.x minor may be incompatible, so being newer than an accepted entry proves nothing.
    ///
    /// It held 0.6.0, 0.7.0 and 0.8.0 until this binding began consuming routes CNA added after
    /// them -- the render-target ContentLost subscription, the two optioned raw vertex uploads, the
    /// caller-owned device pair and the engine-layer availability pair. A library from one of those
    /// generations does not export those names, so admitting it would only move the failure from
    /// load time to first use. 0.19.0 followed them out when 0.20.0 removed eleven renderer
    /// identities: nothing this consumer touches changed, but a point matrix that kept every
    /// generation it had ever accepted would stop being a review and start being a range. See
    /// docs/native-abi-compatibility.md for the retired matrix and the evidence behind each entry.
    /// </summary>
    private static readonly CnaNativeAbiProfile[] Profiles =
    [
        new(ConsumerVersion, "exact", "the ABI generation this CNA.NET consumer was compiled against"),
    ];

    private static readonly Lazy<string[]> RequiredSymbolNames = new(BuildRequiredSymbolNames);

    internal static IReadOnlyList<CnaNativeAbiProfile> AcceptedProfiles => Profiles;

    /// <summary>Every native entry point declared by <see cref="Native"/>, sorted and unique.</summary>
    internal static IReadOnlyList<string> RequiredSymbols => RequiredSymbolNames.Value;

    internal static bool TryGetProfile(uint version, out CnaNativeAbiProfile profile)
    {
        foreach (CnaNativeAbiProfile candidate in Profiles)
        {
            if (candidate.Version == version)
            {
                profile = candidate;
                return true;
            }
        }

        profile = default;
        return false;
    }

    internal static string ExplainRejection(uint version)
    {
        (int major, int minor, int patch) = Decode(version);
        (int consumerMajor, _, _) = Decode(ConsumerVersion);
        if (major != consumerMajor)
        {
            return $"major {major} differs from consumer major {consumerMajor}";
        }

        return version == 0
            ? "the metadata encodes 0.0.0, which is not a complete CNA C ABI generation"
            : $"experimental ABI {major}.{minor}.{patch} is not in the audited compatibility matrix";
    }

    internal static (int Major, int Minor, int Patch) Decode(uint encoded) =>
        ((int)((encoded >> 16) & 0xFFFF), (int)((encoded >> 8) & 0xFF), (int)(encoded & 0xFF));

    private static string[] BuildRequiredSymbolNames()
    {
        return typeof(Native)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(method => (Method: method, Import: method.GetCustomAttribute<LibraryImportAttribute>()))
            .Where(item => string.Equals(item.Import?.LibraryName, "cna-native", StringComparison.Ordinal))
            .Select(item => string.IsNullOrWhiteSpace(item.Import!.EntryPoint)
                ? item.Method.Name
                : item.Import.EntryPoint!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}

internal readonly record struct CnaNativeAbiProfile(uint Version, string Compatibility, string Evidence);
