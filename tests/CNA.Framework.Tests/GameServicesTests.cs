using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// <see cref="GameServiceContainer"/> and <see cref="LaunchParameters"/> are the two Phase 8 WP7a
/// types with no native dependency, so unlike the component types they are directly testable. Both
/// have XNA behaviours that are easy to "improve" by accident, which is what these pin.
/// </summary>
public class GameServiceContainerTests
{
    private interface IThing;

    private sealed class Thing : IThing;

    [Fact]
    public void AddService_ThenGetService_ByInterface_RoundTrips()
    {
        var services = new GameServiceContainer();
        var thing = new Thing();

        services.AddService(typeof(IThing), thing);

        Assert.Same(thing, services.GetService(typeof(IThing)));
    }

    /// <summary>Real XNA looks a service up by the exact type it was registered under -- no
    /// walking of base classes or interfaces. Registering the concrete class and asking for the
    /// interface must miss.</summary>
    [Fact]
    public void GetService_IsExactTypeOnly_NoInheritanceWalk()
    {
        var services = new GameServiceContainer();
        services.AddService(typeof(Thing), new Thing());

        Assert.Null(services.GetService(typeof(IThing)));
    }

    [Fact]
    public void GetService_Unregistered_ReturnsNullRatherThanThrowing() =>
        Assert.Null(new GameServiceContainer().GetService(typeof(IThing)));

    /// <summary>A provider that is not actually an instance of the registered type must fail at
    /// registration, not much later at whatever cast consumed it.</summary>
    [Fact]
    public void AddService_MismatchedProvider_Throws()
    {
        var services = new GameServiceContainer();

        Assert.Throws<ArgumentException>(() => services.AddService(typeof(IThing), "not a thing"));
    }

    [Fact]
    public void AddService_SameTypeTwice_Throws()
    {
        var services = new GameServiceContainer();
        services.AddService(typeof(IThing), new Thing());

        Assert.Throws<ArgumentException>(() => services.AddService(typeof(IThing), new Thing()));
    }

    [Fact]
    public void RemoveService_MakesItUnresolvableAgain()
    {
        var services = new GameServiceContainer();
        services.AddService(typeof(IThing), new Thing());

        services.RemoveService(typeof(IThing));

        Assert.Null(services.GetService(typeof(IThing)));
    }

    [Fact]
    public void RemoveService_Unregistered_IsANoOp()
    {
        var services = new GameServiceContainer();

        services.RemoveService(typeof(IThing));
    }

    [Fact]
    public void GenericOverloads_AgreeWithTheTypeOnes()
    {
        var services = new GameServiceContainer();
        var thing = new Thing();

        services.AddService<IThing>(thing);

        Assert.Same(thing, services.GetService<IThing>());
    }

    [Fact]
    public void ImplementsIServiceProvider()
    {
        IServiceProvider services = new GameServiceContainer();
        var thing = new Thing();
        ((GameServiceContainer)services).AddService(typeof(IThing), thing);

        Assert.Same(thing, services.GetService(typeof(IThing)));
    }
}

public class LaunchParametersTests
{
    [Fact]
    public void Parses_DashKeyValue()
    {
        var parameters = new LaunchParameters(["-width:1280", "-height:720"]);

        Assert.Equal("1280", parameters["width"]);
        Assert.Equal("720", parameters["height"]);
    }

    [Fact]
    public void Parses_SlashPrefixToo()
    {
        var parameters = new LaunchParameters(["/fullscreen:true"]);

        Assert.Equal("true", parameters["fullscreen"]);
    }

    [Fact]
    public void FlagWithoutValue_BecomesEmptyString()
    {
        var parameters = new LaunchParameters(["-verbose"]);

        Assert.True(parameters.ContainsKey("verbose"));
        Assert.Equal(string.Empty, parameters["verbose"]);
    }

    [Fact]
    public void UnprefixedArguments_AreSkipped()
    {
        var parameters = new LaunchParameters(["game.exe", "somefile.txt", "-real:1"]);

        Assert.Single(parameters);
        Assert.Equal("1", parameters["real"]);
    }

    /// <summary>Keys are case-insensitive, matching how XNA-era launch parameters were typically
    /// consumed on Windows.</summary>
    [Fact]
    public void Keys_AreCaseInsensitive()
    {
        var parameters = new LaunchParameters(["-Width:800"]);

        Assert.Equal("800", parameters["width"]);
        Assert.Equal("800", parameters["WIDTH"]);
    }

    /// <summary>A value containing further colons keeps them -- only the first splits.</summary>
    [Fact]
    public void OnlyTheFirstColonSplits()
    {
        var parameters = new LaunchParameters(["-url:http://example.com:8080"]);

        Assert.Equal("http://example.com:8080", parameters["url"]);
    }

    [Fact]
    public void DefaultConstructor_IsEmpty() => Assert.Empty(new LaunchParameters());
}
