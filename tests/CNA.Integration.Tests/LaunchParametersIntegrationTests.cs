using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// <c>Game.LaunchParameters</c>, now that it enumerates native instead of re-parsing the process
/// command line.
///
/// The divergence this closes is narrow and was real: a parameter added through
/// <c>AddLaunchParameter</c> at run time reached native and never appeared in the dictionary, so
/// <c>ContainsLaunchParameter(key)</c> could answer true for a key <c>LaunchParameters</c> did not
/// contain. Only a test that adds one and then enumerates can see it -- which is what these do.
/// </summary>
public class LaunchParametersIntegrationTests(ITestOutputHelper output)
{
    [NativeFact]
    public void LaunchParameters_IncludeAParameterAddedAtRunTime()
    {
        using var game = new CNA.Game();

        game.AddLaunchParameter("integration-added", "value-1");

        LaunchParameters parameters = game.LaunchParameters;
        output.WriteLine($"{parameters.Count} parameter(s): {string.Join(", ", parameters.Keys)}");

        Assert.True(
            parameters.ContainsKey("integration-added"),
            "A run-time addition is missing from the dictionary -- LaunchParameters is not reading native.");
        Assert.Equal("value-1", parameters["integration-added"]);
    }

    /// <summary>The dictionary and the keyed accessors must agree. They are separate ABI routes,
    /// and the whole point of enumerating native is that they can no longer disagree.</summary>
    [NativeFact]
    public void LaunchParameters_AgreeWithTheKeyedAccessors()
    {
        using var game = new CNA.Game();

        game.AddLaunchParameter("alpha", "one");
        game.AddLaunchParameter("beta", "two");

        LaunchParameters parameters = game.LaunchParameters;

        foreach ((string key, string value) in parameters)
        {
            Assert.True(game.ContainsLaunchParameter(key), $"'{key}' is enumerated but ContainsLaunchParameter says no.");
            Assert.Equal(value, game.GetLaunchParameter(key));
        }
    }

    /// <summary>
    /// The ABI sorts keys by name, ordinal ascending, and says so explicitly -- because the
    /// underlying container is a hash map whose order is unspecified and where one insertion can
    /// rehash everything.
    ///
    /// Asserted here because the guarantee is what makes an index meaningful at all, and because a
    /// dictionary hides it: <c>LaunchParameters</c> is a <c>Dictionary</c>, so its own enumeration
    /// order proves nothing. Adding keys in reverse order and checking the count is what catches a
    /// walk that skipped or repeated an entry.
    /// </summary>
    [NativeFact]
    public void LaunchParameters_EnumerateEveryKeyExactlyOnce()
    {
        using var game = new CNA.Game();

        string[] added = ["zulu", "alpha", "mike", "bravo"];
        foreach (string key in added)
        {
            game.AddLaunchParameter(key, key.ToUpperInvariant());
        }

        LaunchParameters parameters = game.LaunchParameters;

        foreach (string key in added)
        {
            Assert.Equal(key.ToUpperInvariant(), parameters[key]);
        }

        Assert.Equal(added.Length, added.Distinct().Count());
        Assert.True(
            parameters.Count >= added.Length,
            $"Expected at least {added.Length} parameters, enumerated {parameters.Count}.");
    }
}
