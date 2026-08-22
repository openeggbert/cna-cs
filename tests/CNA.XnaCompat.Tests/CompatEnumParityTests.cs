using System.Reflection;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Every enum in <c>Microsoft.Xna.Framework.*</c> is a duplicate rather than an alias. Most CNA
/// counterparts deliberately share the same values, but the strict facade must follow XNA even
/// where the CNA/native representation differs. Those exceptions are enumerated with reasons
/// below so this test rejects both accidental new divergence and obsolete exceptions.
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

    private static readonly Dictionary<string, string> KnownMemberDivergences = new()
    {
        [typeof(Microsoft.Xna.Framework.Graphics.BlendFunction).FullName!] =
            "XNA assigns Min=3 and Max=4; the CNA/native enum uses Max=3 and Min=4.",
        [typeof(Microsoft.Xna.Framework.Graphics.SurfaceFormat).FullName!] =
            "CNA adds post-XNA formats; the strict facade exposes only XNA's 0-19 set.",
        [typeof(Microsoft.Xna.Framework.Input.Buttons).FullName!] =
            "XNA includes virtual thumbstick-direction and trigger flags absent from CNA's physical-button enum.",
        [typeof(Microsoft.Xna.Framework.Input.GamePadType).FullName!] =
            "XNA assigns BigButtonPad=0x300 while CNA's native ABI represents it as 9.",
        [typeof(Microsoft.Xna.Framework.Input.Keys).FullName!] =
            "The strict facade follows the exact XNA key set; CNA also exposes native-only key names.",
    };

    private static readonly Dictionary<string, string> KnownFlagsAttributeDivergences = new()
    {
        [typeof(Microsoft.Xna.Framework.Graphics.BufferUsage).FullName!] =
            "XNA marks BufferUsage with FlagsAttribute; the CNA counterpart currently does not.",
        [typeof(Microsoft.Xna.Framework.Graphics.SetDataOptions).FullName!] =
            "XNA marks SetDataOptions with FlagsAttribute; the CNA counterpart currently does not.",
    };

    [Theory]
    [InlineData(CNA.Graphics.BlendFunction.Min, Microsoft.Xna.Framework.Graphics.BlendFunction.Min)]
    [InlineData(CNA.Graphics.BlendFunction.Max, Microsoft.Xna.Framework.Graphics.BlendFunction.Max)]
    [InlineData(CNA.Graphics.BlendFunction.Add, Microsoft.Xna.Framework.Graphics.BlendFunction.Add)]
    public void BlendFunctionConversions_MapSemanticsRatherThanDivergentOrdinals(
        CNA.Graphics.BlendFunction framework,
        Microsoft.Xna.Framework.Graphics.BlendFunction compat)
    {
        Assert.Equal(
            compat,
            InvokeInternalConversion<CNA.Graphics.BlendFunction, Microsoft.Xna.Framework.Graphics.BlendFunction>(
                "Microsoft.Xna.Framework.Graphics.BlendFunctionConversions", "ToCompat", framework));
        Assert.Equal(
            framework,
            InvokeInternalConversion<Microsoft.Xna.Framework.Graphics.BlendFunction, CNA.Graphics.BlendFunction>(
                "Microsoft.Xna.Framework.Graphics.BlendFunctionConversions", "ToFramework", compat));
    }

    [Theory]
    [InlineData(CNA.Input.GamePadType.BigButtonPad, Microsoft.Xna.Framework.Input.GamePadType.BigButtonPad)]
    [InlineData(CNA.Input.GamePadType.GamePad, Microsoft.Xna.Framework.Input.GamePadType.GamePad)]
    public void GamePadTypeConversion_MapsTheDivergentBigButtonPadOrdinal(
        CNA.Input.GamePadType framework,
        Microsoft.Xna.Framework.Input.GamePadType compat) =>
        Assert.Equal(
            compat,
            InvokeInternalConversion<CNA.Input.GamePadType, Microsoft.Xna.Framework.Input.GamePadType>(
                "Microsoft.Xna.Framework.Input.GamePadTypeConversions", "ToCompat", framework));

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
    public void CompatEnum_MatchesItsCnaCounterpart_ExceptForReviewedXnaDifferences(Type compatEnum)
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

        bool membersAreIdentical = cnaMembers.OrderBy(p => p.Key, StringComparer.Ordinal)
            .SequenceEqual(compatMembers.OrderBy(p => p.Key, StringComparer.Ordinal));
        bool divergenceIsExpected = KnownMemberDivergences.ContainsKey(compatEnum.FullName!);

        Assert.Equal(!divergenceIsExpected, membersAreIdentical);
    }

    /// <summary>The <c>[Flags]</c> attribute has to travel with the duplicate too -- it changes
    /// <c>ToString</c>/<c>HasFlag</c> behavior, so a mirror that drops it is wrong in a way the
    /// value comparison above cannot see.</summary>
    [Theory]
    [MemberData(nameof(CompatEnums))]
    public void CompatEnum_FlagsAttribute_MatchesItsCnaCounterpart_ExceptForReviewedXnaDifferences(Type compatEnum)
    {
        Type? cnaEnum = FindCnaCounterpart(compatEnum);
        if (cnaEnum is null)
        {
            return;
        }

        bool attributesAreIdentical =
            cnaEnum.IsDefined(typeof(FlagsAttribute), inherit: false) ==
            compatEnum.IsDefined(typeof(FlagsAttribute), inherit: false);
        bool divergenceIsExpected = KnownFlagsAttributeDivergences.ContainsKey(compatEnum.FullName!);

        Assert.Equal(!divergenceIsExpected, attributesAreIdentical);
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

    private static TOutput InvokeInternalConversion<TInput, TOutput>(
        string typeName,
        string methodName,
        TInput value)
    {
        Type converter = typeof(Microsoft.Xna.Framework.Graphics.SpriteEffects).Assembly.GetType(
            typeName,
            throwOnError: true)!;
        MethodInfo method = converter.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
        return Assert.IsType<TOutput>(method.Invoke(null, [value]));
    }
}
