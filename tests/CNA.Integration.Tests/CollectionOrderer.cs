using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CNA.Integration.Tests;

/// <summary>
/// Runs the tests that build their own games before the ones that share one.
///
/// Ordering is load-bearing here for a reason outside this assembly's control: native allows one
/// C-owned game at a time and refuses to destroy a game while a resource created against it is
/// alive. The shared fixture's game accumulates such resources across a whole collection --
/// including ones whose XNA counterparts are not <c>IDisposable</c>, so nothing disposes them and
/// only a finalizer would -- and xunit disposes a collection fixture after that collection
/// finishes. Whether the slot is free when the own-game tests run therefore depends on collection
/// order, which xunit does not otherwise fix.
///
/// Measured before this existed: the same five lifecycle tests failed in roughly half of runs, all
/// with "All owned C child resources must be destroyed before the game" -- pointing at the tests
/// that were merely unlucky in the order rather than at anything they did.
///
/// Running them first sidesteps it entirely: nothing has been created yet, so the slot is free.
/// </summary>
public sealed class CollectionOrderer : ITestCollectionOrderer
{
    public const string TypeName = "CNA.Integration.Tests.CollectionOrderer";

    public const string AssemblyName = "CNA.Integration.Tests";

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections.OrderBy(collection => collection.DisplayName.Contains(OwnGameCollection.Name, StringComparison.Ordinal) ? 0 : 1)
                       .ThenBy(collection => collection.DisplayName, StringComparer.Ordinal);
}
