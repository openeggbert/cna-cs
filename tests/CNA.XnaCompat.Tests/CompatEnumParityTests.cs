using System.Reflection;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Every enum in <c>Microsoft.Xna.Framework.*</c> is a *duplicate* of a <c>CNA.*</c> one rather
/// than an alias (C# forbids user-defined conversions on enums, so the two namespaces cast by
/// value across the boundary -- see <c>SpriteEffects</c>'s own doc comment). That duplication is
/// only safe while the two stay numerically identical, and nothing in the compiler enforces it:
/// a member added, removed, renamed, or renumbered on one side alone is a silent wrong value at
/// every cast site, not a build error.
///
/// Rather than restating every member by hand (which would itself drift), this walks the compat
/// assembly by reflection, pairs each enum with its same-named <c>CNA.*</c> counterpart, and
/// compares the whole member set. It therefore also covers enums added *after* this file was
/// written, with no edit here -- the failure mode it guards against is exactly the one a
/// hand-maintained list would miss.
/// </summary>
public class CompatEnumParityTests
{
    /// <summary>Compat enums with no same-named CNA counterpart at all. Each entry is a deliberate
    /// asymmetry, not an oversight, and needs a reason.</summary>
    private static readonly Dictionary<string, string> KnownUnpairedCompatEnums = new()
    {
        // (none today -- every compat enum currently mirrors a CNA.* one)
    };

    public static TheoryData<Type> CompatEnums()
    {
        var data = new TheoryData<Type>();
        foreach (Type type in typeof(Microsoft.Xna.Framework.Graphics.SpriteEffects).Assembly
                     .GetExportedTypes()
                     .Where(t => t.IsEnum && t.Namespace is not null && t.Namespace.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal))
                     .OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CompatEnums))]
    public void CompatEnum_HasIdenticalMembersAndValues_ToItsCnaCounterpart(Type compatEnum)
    {
        Type? cnaEnum = FindCnaCounterpart(compatEnum);

        if (cnaEnum is null)
        {
            Assert.True(
                KnownUnpairedCompatEnums.ContainsKey(compatEnum.Name),
                $"{compatEnum.FullName} has no CNA.* counterpart and is not listed as a deliberate asymmetry. " +
                "Either add the CNA.* enum it should mirror, or document it in KnownUnpairedCompatEnums.");
            return;
        }

        Dictionary<string, long> compatMembers = ReadMembers(compatEnum);
        Dictionary<string, long> cnaMembers = ReadMembers(cnaEnum);

        Assert.Equal(cnaMembers.OrderBy(p => p.Key, StringComparer.Ordinal), compatMembers.OrderBy(p => p.Key, StringComparer.Ordinal));
    }

    /// <summary>The <c>[Flags]</c> attribute has to travel with the duplicate too -- it changes
    /// <c>ToString</c>/<c>HasFlag</c> behavior, so a mirror that drops it is wrong in a way the
    /// value comparison above cannot see.</summary>
    [Theory]
    [MemberData(nameof(CompatEnums))]
    public void CompatEnum_FlagsAttribute_MatchesItsCnaCounterpart(Type compatEnum)
    {
        Type? cnaEnum = FindCnaCounterpart(compatEnum);
        if (cnaEnum is null)
        {
            return;
        }

        Assert.Equal(
            cnaEnum.IsDefined(typeof(FlagsAttribute), inherit: false),
            compatEnum.IsDefined(typeof(FlagsAttribute), inherit: false));
    }

    /// <summary>Pairs by simple name across the two namespace trees. The compat tree mirrors the
    /// CNA tree's own layout closely but not identically (e.g. <c>CNA.DisplayOrientation</c> is in
    /// the root namespace while several graphics enums are not), so this searches the whole CNA
    /// assembly by name rather than assuming a namespace mapping.</summary>
    private static Type? FindCnaCounterpart(Type compatEnum) =>
        typeof(CNA.Graphics.SpriteEffects).Assembly
            .GetExportedTypes()
            .SingleOrDefault(t => t.IsEnum
                && t.Name == compatEnum.Name
                && t.Namespace is not null
                && (t.Namespace == "CNA" || t.Namespace.StartsWith("CNA.", StringComparison.Ordinal)));

    private static Dictionary<string, long> ReadMembers(Type enumType) =>
        enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(
                f => f.Name,
                f => Convert.ToInt64(f.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture),
                StringComparer.Ordinal);
}
