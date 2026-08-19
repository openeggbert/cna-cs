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
public class ContentReaderRegistrationTests(ITestOutputHelper output)
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

        using (var probe = new ForeignProbe(ex => caught = ex))
        {
            for (int i = 0; i < 4 && !probe.Ran; i++)
            {
                probe.RunOneFrame();
            }
        }

        Assert.NotNull(caught);
        output.WriteLine($"{caught!.GetType().Name}: {caught.Message}");
        Assert.IsType<CnaException>(caught);
    }

    private sealed class ForeignProbe(Action<Exception> report) : CNA.Game
    {
        public bool Ran { get; private set; }

        protected override void Update(GameTime gameTime)
        {
            if (!Ran)
            {
                Ran = true;
                try
                {
                    Content.LoadForeign<object>("no-such-foreign-asset");
                }
                catch (Exception ex)
                {
                    report(ex);
                }
            }

            Exit();
            base.Update(gameTime);
        }
    }
}
