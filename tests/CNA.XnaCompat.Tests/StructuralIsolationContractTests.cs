using System.Reflection;
using Microsoft.Xna.Framework;
using Xunit;

namespace CNA.XnaCompat.Tests;

public class StructuralIsolationContractTests
{
    private static readonly Assembly CompatAssembly = typeof(Microsoft.Xna.Framework.Game).Assembly;

    [Fact]
    public void ExportedCompatTypes_DoNotInheritFromCnaTypes()
    {
        Type[] offenders = CompatAssembly.GetExportedTypes()
            .Where(type => IsCnaType(type.BaseType))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void PublicConversionOperators_DoNotMentionCnaTypes()
    {
        MethodInfo[] offenders = CompatAssembly.GetExportedTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.Name is "op_Implicit" or "op_Explicit")
            .Where(method => IsCnaType(method.ReturnType) || method.GetParameters().Any(parameter => IsCnaType(parameter.ParameterType)))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData("Microsoft.Xna.Framework.Graphics.NamedModelCollection`1")]
    [InlineData("Microsoft.Xna.Framework.Media.MediaLibraryObject`1")]
    [InlineData("Microsoft.Xna.Framework.Media.ReadOnlyMediaCollection`2")]
    public void ImplementationHelpers_AreNotExported(string fullName)
    {
        Assert.DoesNotContain(CompatAssembly.GetExportedTypes(), type => type.FullName == fullName);
    }

    private static bool IsCnaType(Type? type) =>
        type?.Namespace is { } namespaceName &&
        (namespaceName == "CNA" || namespaceName.StartsWith("CNA.", StringComparison.Ordinal));
}
