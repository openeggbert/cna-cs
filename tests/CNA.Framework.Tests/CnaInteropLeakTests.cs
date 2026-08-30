using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Tests;

/// <summary>
/// Design invariant 5 -- no <c>CNA.Interop</c> type in a public or protected signature -- is
/// enforced by the C# compiler for <c>CNA.Framework</c>, and this pins the precondition that makes
/// that true.
///
/// <b>The direct test was written first and thrown away, because it could not fail.</b> Planting a
/// public member returning <c>CnaHandle</c> does not produce a leak the test catches; it produces
/// <c>CS0050: inconsistent accessibility</c>, because every interop type is <c>internal</c>. Trying
/// again with the one exported type produces <c>CS0722: static types cannot be used as return
/// types</c>. There is no way to write the violation.
///
/// So the thing worth guarding is not the absence of leaks -- it is the property that makes leaks
/// unwritable. <c>CNA.Interop</c> exports exactly one type and it is a static class, so nothing it
/// declares can appear in a signature at all. Add a public struct or enum to that assembly and the
/// compiler stops helping; this fails then, which is the moment somebody should be asked whether
/// they meant to.
///
/// <c>tools/api-compat</c>'s own leak gate remains the check for the strict XNA profile, where the
/// question is different: there the types are public by necessity and the gate is what keeps a CNA
/// type out of an XNA signature.
/// </summary>
public class CnaInteropLeakTests(ITestOutputHelper output)
{
    [Fact]
    public void InteropExportsNothingThatCanAppearInASignature()
    {
        Assembly interop = typeof(CNA.Interop.NativeLibraryResolver).Assembly;
        Type[] exported = interop.GetExportedTypes();

        foreach (Type type in exported)
        {
            output.WriteLine($"{type.FullName} static={type is { IsAbstract: true, IsSealed: true }}");
        }

        // A static class -- abstract and sealed in metadata -- cannot be a parameter, return, field
        // or property type. Anything else exported here could be, and the compiler would then permit
        // exactly the leak invariant 5 forbids.
        Assert.All(exported, type => Assert.True(
            type is { IsAbstract: true, IsSealed: true },
            $"{type.FullName} is exported from CNA.Interop and is not a static class, so it can " +
            "appear in a public signature. Invariant 5 stops being compiler-enforced."));

        // The resolver is public on purpose: a host needs to point the loader at a library. Named
        // so that a second export has to justify itself rather than slip in beside it.
        Assert.Equal(["CNA.Interop.NativeLibraryResolver"], exported.Select(type => type.FullName));
    }
}
