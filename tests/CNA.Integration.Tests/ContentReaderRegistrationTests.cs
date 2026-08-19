using CNA.Content;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Custom content-reader registration -- the last of the four gaps this binding reported upstream,
/// and the one whose absence made the content pipeline "extensible in name only".
///
/// What can be tested without a compiled asset is the registration half, and that is worth testing
/// on its own: it is where the callback table crosses the ABI, and a wrong struct layout or calling
/// convention there corrupts the stack rather than throwing. Reading an actual custom <c>.xnb</c>
/// needs a fixture built by the XNA content pipeline, which this repository has no way to produce;
/// that is stated here rather than papered over with a test that proves less than it appears to.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class ContentReaderRegistrationTests(ITestOutputHelper output, NativeGameFixture fixture)
{
    private sealed class StubReader : ManagedContentTypeReader
    {
        public override object Read(ContentReader input, object? existingInstance) =>
            new { Marker = "never reached in these tests" };
    }

    private static string UniqueName(string suffix) =>
        $"CNA.Integration.Tests.StubReader.{suffix}, CNA.Integration.Tests, Version=1.0.0.0";

    [NativeFact]
    public void Register_ReachesTheNativeRegistry_AndIsVisibleToIsRegistered()
    {
        string name = UniqueName("visible");

        Assert.False(ContentTypeReaderManager.IsRegistered(name), "The name was already taken before registering.");

        using (ContentTypeReaderRegistration.Register(name, "CNA.Integration.Tests.StubTarget", () => new StubReader()))
        {
            Assert.True(
                ContentTypeReaderManager.IsRegistered(name),
                "Registration succeeded but the registry does not report the name -- the callback table did not land.");
        }

        Assert.False(
            ContentTypeReaderManager.IsRegistered(name),
            "Disposing the registration did not withdraw the factory.");
    }

    /// <summary>
    /// A duplicate must be refused. That is a deliberate deviation from the canonical
    /// <c>AddTypeCreator</c>, which silently ignores a repeat -- right for built-in readers
    /// registering themselves twice through two static-initialisation paths, and wrong here: a
    /// caller who took a name someone else owned would otherwise hold a live registration whose
    /// factory is never called, and would find out only from assets deserializing into the wrong
    /// type.
    /// </summary>
    [NativeFact]
    public void Register_RefusesADuplicateName()
    {
        string name = UniqueName("duplicate");

        using ContentTypeReaderRegistration first =
            ContentTypeReaderRegistration.Register(name, "CNA.Integration.Tests.StubTarget", () => new StubReader());

        var thrown = Assert.Throws<CnaException>(() =>
            ContentTypeReaderRegistration.Register(name, "CNA.Integration.Tests.StubTarget", () => new StubReader()));

        output.WriteLine(thrown.Message);
        Assert.Contains("InvalidState", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>The name is free again afterwards, which is what makes a withdrawn registration
    /// actually withdrawn rather than merely detached.</summary>
    [NativeFact]
    public void Register_AfterDispose_TheNameIsFreeAgain()
    {
        string name = UniqueName("reused");

        ContentTypeReaderRegistration.Register(name, "CNA.Integration.Tests.StubTarget", () => new StubReader())
            .Dispose();

        using ContentTypeReaderRegistration second =
            ContentTypeReaderRegistration.Register(name, "CNA.Integration.Tests.StubTarget", () => new StubReader());

        Assert.Equal(name, second.CanonicalName);
    }

    /// <summary>Disposing twice must be safe -- the registration is process-wide and nothing stops
    /// a caller from disposing it on two paths.</summary>
    [NativeFact]
    public void Dispose_IsIdempotent()
    {
        var registration = ContentTypeReaderRegistration.Register(
            UniqueName("idempotent"), "CNA.Integration.Tests.StubTarget", () => new StubReader());

        registration.Dispose();
        registration.Dispose();
    }

    /// <summary>
    /// <c>LoadForeign</c> is wired, and a missing asset fails as IO rather than as an unsupported
    /// type. Same distinction the effect loader's test draws, for the same reason: it separates
    /// "the route exists and the file does not" from "nothing handles this".
    /// </summary>
    [NativeFact]
    public void LoadForeign_IsWired_AndReportsAMissingAssetAsIo()
    {
        Exception? caught = null;

        fixture.InsideAFrame(game =>
        {
            try
            {
                game.Content.LoadForeign<object>("no-such-foreign-asset");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        Assert.NotNull(caught);
        output.WriteLine($"{caught!.GetType().Name}: {caught.Message}");
        Assert.IsType<CnaException>(caught);
    }

    private sealed class StubCnjLoader : CnjLoader
    {
        public string? SawJson { get; private set; }

        public override object Load(string descriptorJson)
        {
            SawJson = descriptorJson;
            return new Marker(descriptorJson.Length);
        }
    }

    private sealed record Marker(int JsonLength);

    /// <summary>
    /// The <c>.cnj</c> loader registry, added upstream after this project's framing about
    /// limitation-asserting claims prompted an audit that found the row saying it was impossible
    /// had outlived its own premise.
    ///
    /// It is the counterpart of the reader registry above, with one deliberate difference worth
    /// asserting: registration is per content manager and has no unregister, because native's table
    /// belongs to the manager and dies with it.
    /// </summary>
    [NativeFact]
    public void RegisterCnjLoader_ReachesNative_AndRefusesADuplicateTypeName()
    {
        fixture.InsideAFrame(game =>
        {
            var loader = new StubCnjLoader();
            CnjLoaderRegistration registration =
                game.Content.RegisterCnjLoader("CnaIntegrationTestsMarker", loader);

            Assert.Equal("CnaIntegrationTestsMarker", registration.TypeName);

            // Per-manager and already taken: the second attempt must be refused rather than
            // silently shadowing the first, the same rule the reader registry follows.
            var duplicate = Assert.Throws<CnaException>(() =>
                game.Content.RegisterCnjLoader("CnaIntegrationTestsMarker", new StubCnjLoader()));

            output.WriteLine(duplicate.Message);
            Assert.Contains("InvalidState", duplicate.Message, StringComparison.Ordinal);
        });
    }

    /// <summary>A descriptor naming a type nothing is registered for must fail the load rather than
    /// fall back -- the same rule a compiled asset naming an unregistered reader follows.</summary>
    [NativeFact]
    public void LoadForeign_WithNoRegisteredCnjType_Fails()
    {
        Exception? caught = null;

        fixture.InsideAFrame(game =>
        {
            try
            {
                game.Content.LoadForeign<object>("no-such-cnj-descriptor");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        Assert.NotNull(caught);
        output.WriteLine($"{caught!.GetType().Name}: {caught.Message}");
    }
}
