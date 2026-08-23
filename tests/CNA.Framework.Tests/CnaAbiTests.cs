using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CNA.Interop;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="CnaAbi.Decode"/> is pure arithmetic over the encoding <c>abi.h</c> defines, so it is
/// testable without a native library -- and worth testing, because a wrong shift or mask produces a
/// plausible-looking version rather than an obvious failure, which is exactly the situation the
/// check exists to prevent.
///
/// Native admission runs in isolated fixture processes. These tests pin its pure version matrix
/// and prove that its required-symbol set is the complete managed import surface.
/// </summary>
public class CnaAbiTests
{
    /// <summary>The encoding is <c>(major &amp; 0xFFFF) &lt;&lt; 16 | (minor &amp; 0xFF) &lt;&lt; 8 |
    /// (patch &amp; 0xFF)</c> -- 16 bits for major, 8 each for the rest, which is asymmetric and the
    /// easiest part to get wrong.</summary>
    [Theory]
    [InlineData(0u, 0, 0, 0)]
    [InlineData((1u << 8), 0, 1, 0)]
    [InlineData((1u << 16), 1, 0, 0)]
    [InlineData(1u, 0, 0, 1)]
    [InlineData((2u << 16) | (3u << 8) | 4u, 2, 3, 4)]
    [InlineData((0xFFFFu << 16) | (0xFFu << 8) | 0xFFu, 0xFFFF, 0xFF, 0xFF)]
    public void Decode_SplitsTheFieldsAtTheDocumentedWidths(uint encoded, int major, int minor, int patch)
    {
        Assert.Equal((major, minor, patch), CnaAbi.Decode(encoded));
    }

    /// <summary>
    /// The constant this binding compares against must be the version it was written for, now
    /// 0.6.0 -- 0.2.0 added the content-reader registration, SpriteFont and launch-parameter
    /// routes, 0.3.0 tightened CNA_Bool to reject any byte outside {0, 1}, 0.4.0 added the
    /// <c>.cnj</c> loader registration, 0.5.0 the native-window accessor, and 0.6.0 the empty-shader-source refusal.
    ///
    /// Updating this alongside the constant is the point: a constant that drifts silently would
    /// make the compatibility check pass against a library it should reject. Only the *major*
    /// component gates that check, so this pin is about keeping the recorded number honest rather
    /// than about compatibility itself.
    /// </summary>
    [Fact]
    public void ExpectedVersion_IsTheAbiThisBindingWasWrittenAgainst()
    {
        Assert.Equal((0, 6, 0), CnaAbi.Decode(CnaAbi.ExpectedVersion));
    }

    /// <summary>Round-trips every field independently, so a mask that swallowed a neighbouring
    /// field's bits would show up rather than cancel out.</summary>
    [Fact]
    public void Decode_FieldsDoNotBleedIntoEachOther()
    {
        Assert.Equal((0, 0, 0xFF), CnaAbi.Decode(0xFFu));
        Assert.Equal((0, 0xFF, 0), CnaAbi.Decode(0xFF00u));
        Assert.Equal((0xFFFF, 0, 0), CnaAbi.Decode(0xFFFF0000u));
    }

    [Theory]
    [InlineData(0, 6, 0, "exact")]
    [InlineData(0, 7, 0, "additive")]
    [InlineData(0, 8, 0, "reviewed-consumer-subset")]
    public void Policy_AcceptsOnlyReviewedAbiGenerations(int major, int minor, int patch, string classification)
    {
        uint version = ((uint)major << 16) | ((uint)minor << 8) | (uint)patch;
        Assert.True(CnaNativeAbiPolicy.TryGetProfile(version, out CnaNativeAbiProfile profile));
        Assert.Equal(classification, profile.Compatibility);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 5, 0)]
    [InlineData(0, 6, 1)]
    [InlineData(0, 8, 1)]
    [InlineData(0, 9, 0)]
    [InlineData(1, 0, 0)]
    public void Policy_RejectsUnauditedVersions(int major, int minor, int patch)
    {
        uint version = ((uint)major << 16) | ((uint)minor << 8) | (uint)patch;
        Assert.False(CnaNativeAbiPolicy.TryGetProfile(version, out _));
    }

    [Fact]
    public void RequiredSymbols_AreEveryDeclaredNativeImport()
    {
        string[] declared = typeof(Native)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Select(method => (Method: method, Import: method.GetCustomAttribute<LibraryImportAttribute>()))
            .Where(item => string.Equals(item.Import?.LibraryName, "cna-native", StringComparison.Ordinal))
            .Select(item => string.IsNullOrWhiteSpace(item.Import!.EntryPoint)
                ? item.Method.Name
                : item.Import.EntryPoint!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(841, declared.Length);
        Assert.Equal(declared, CnaNativeAbiPolicy.RequiredSymbols);
    }

    [Fact]
    public void MachineReadablePolicy_MatchesExecutableMatrix()
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cna-native-abi-policy.json")));
        JsonElement root = document.RootElement;

        Assert.Equal(CnaNativeAbiPolicy.PolicyVersion, root.GetProperty("policyVersion").GetString());
        Assert.Equal("0.6.0", root.GetProperty("consumerAbi").GetString());
        JsonElement[] entries = root.GetProperty("acceptedVersions").EnumerateArray().ToArray();
        string[] versions = entries.Select(item => item.GetProperty("libraryAbi").GetString()!).ToArray();
        Assert.Equal(
            CnaNativeAbiPolicy.AcceptedProfiles.Select(profile => Format(profile.Version)),
            versions);
        Assert.Equal(
            CnaNativeAbiPolicy.AcceptedProfiles.Select(profile => profile.Compatibility),
            entries.Select(item => item.GetProperty("classification").GetString()));
        Assert.Equal(10, root.GetProperty("fixtures").GetArrayLength());
    }

    private static string Format(uint version)
    {
        (int major, int minor, int patch) = CnaAbi.Decode(version);
        return $"{major}.{minor}.{patch}";
    }
}
